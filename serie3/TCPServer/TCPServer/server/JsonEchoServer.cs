using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TCPServer;
using TCPServer.server;

namespace JsonEchoServer
{
    // To represent a JSON request
    public class Request
    {
        public String Method { get; set; }
        public String Path { get; set; }
        public Dictionary<String, String> Headers { get; set; }
        public JObject Payload { get; set; }

        public override String ToString()
        {
            return $"Method: {Method}, Headers: {Headers}, Payload: {Payload}";
        }
    }

    // To represent a JSON response
    public class Response
    {
        public int Status { get; set; }
        public Dictionary<String, String> Headers { get; set; }
        public JObject Payload { get; set; }
    }
      
    public class Program
    {
        private const int port = 8081;
        private static int counter;

        private static JObject
            nonExistentQueuePayload = createJObject("body", "Queue does not exist"),
            successPayload = createJObject("body", "Success"),
            timeoutPayload = createJObject("body", "Timeout"),
            formatErrorPayload = createJObject("body", "Invalid format"),
            toShutDownPayload = createJObject("body", "Shutdown"),
            serverError = createJObject("body", "Error in server");

        private static JObject createJObject(string property, string value)
        {
            return new JObject(new JProperty(property, value));
        }

        private static ConcurrentDictionary<string, MessageQueue> messageCollections;
        private volatile static bool toShutDown;
        private volatile static int inServiceRequests = 0;
        private static object _lock = new object();

        static async Task Main(string[] args)
        {   
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            Console.WriteLine($"Listening on {port}");

            messageCollections = new ConcurrentDictionary<string, MessageQueue>();
      
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                var id = counter++;
                Console.WriteLine($"connection accepted with id '{id}'");
                Handle(id, client);
            }
        }

        private static readonly JsonSerializer serializer = new JsonSerializer();
        
        private static async void Handle(int id, TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var reader = new JsonTextReader(new StreamReader(stream))
                {
                    // To support reading multiple top-level objects
                    SupportMultipleContent = true
                };
                var writer = new JsonTextWriter(new StreamWriter(stream));
                while (true)
                {
                    try
                    {
                        // to consume any bytes until start of object ('{')
                        do
                        {
                            await reader.ReadAsync();
                            Console.WriteLine($"advanced to {reader.TokenType}");
                        } while (reader.TokenType != JsonToken.StartObject
                                 && reader.TokenType != JsonToken.None);

                        if (reader.TokenType == JsonToken.None)
                        {
                            Console.WriteLine($"[{id}] reached end of input stream, ending.");
                            return;
                        }
                        var json = await JObject.LoadAsync(reader);
                        // to ensure that proper deserialization is possible
                        Request request = json.ToObject<Request>();

                        Response response;

                        switch (request.Method)
                        {
                            case "CREATE": response = Create(request.Path); break;
                            case "SEND": response = await Send(request); break;
                            case "RECEIVE": response = await Receive(request); break;
                            case "SHUTDOWN": response = await ShutDown(request); break;
                            default: response = new Response { Status = 400 }; break;
                        }

                        serializer.Serialize(writer, response);
                        await writer.FlushAsync();
                    }
                    catch (JsonReaderException e)
                    {
                        Console.WriteLine($"[{id}] Error reading JSON: {e.Message}, continuing");
                        var response = new Response
                        {
                            Status = 400,
                        };
                        serializer.Serialize(writer, response);
                        await writer.FlushAsync();
                        // close the connection because an error may not be recoverable by the reader
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{id}] Unexpected exception, closing connection {e.Message}");
                        return;
                    }
                }
            }
        }
        public static Response Create(string path)
        {
            if(toShutDown)
                return new Response { Status = 503, Payload = toShutDownPayload };

            messageCollections.TryGetValue(path, out MessageQueue mQueue);
            if(mQueue == null)
            {
                messageCollections.TryAdd(path, new MessageQueue(10));
            }
            return new Response { Status = 200 , Payload= successPayload };
        }

        public async static Task<Response> Send(Request request) {
            try
            {
                if (toShutDown) return new Response { Status = 503, Payload = toShutDownPayload };
                Interlocked.Increment(ref inServiceRequests);

                var payload = request.Payload;
                messageCollections.TryGetValue(request.Path, out MessageQueue mQueue);
                if (mQueue == null)
                {
                    return new Response { Status = 404, Payload = nonExistentQueuePayload };
                }

                await mQueue.PutAsync(new Message(payload));
                return new Response { Status = 200, Payload = successPayload };
            }
            finally
            {
                lock (_lock)
                {
                    Interlocked.Decrement(ref inServiceRequests);
                    if (inServiceRequests == 0 && toShutDown)
                        Monitor.Pulse(_lock);
                }
            }
        }

        public async static Task<Response> Receive(Request request) {
            try
            {
                if (toShutDown) return new Response { Status = 503, Payload = toShutDownPayload };
                Interlocked.Increment(ref inServiceRequests);

                String tHeader;
                int timeout;
                request.Headers.TryGetValue("timeout", out tHeader);
                if (tHeader == null) { timeout = Timeout.Infinite; }
                else int.TryParse(tHeader, out timeout);

                messageCollections.TryGetValue(request.Path, out MessageQueue mQueue);
                if (mQueue == null)
                {
                    return new Response { Status = 404, Payload = nonExistentQueuePayload };
                }

                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                Message message = await mQueue.TakeAsync(timeout, ct);
                if (message == null)
                {
                    return new Response { Status = 204, Payload = timeoutPayload };
                }

                return new Response { Status = 200, Payload = message.Payload };

            } finally
            {
                lock (_lock)
                {
                    Interlocked.Decrement(ref inServiceRequests);
                    if (inServiceRequests == 0 && toShutDown)
                        Monitor.Pulse(_lock);
                }
            }
        }

        public async static Task<Response> ShutDown(Request request) {

            if (toShutDown) return new Response { Status = 503, Payload = toShutDownPayload };
            toShutDown = true;

            if(inServiceRequests == 0)
                return new Response { Status = 200, Payload = toShutDownPayload };

            String tHeader;
            int timeout;
            request.Headers.TryGetValue("timeout", out tHeader);
            if (tHeader == null) { timeout = Timeout.Infinite; }
            else int.TryParse(tHeader, out timeout);
            TimeoutHolder th = new TimeoutHolder(timeout);
            lock (_lock)
            {
                do
                {
                    try
                    {
                        if ((timeout = th.Value) == 0)
                        {
                            return new Response { Status = 204, Payload = timeoutPayload };
                        }
                        else if (inServiceRequests == 0)
                            return new Response { Status = 200, Payload = toShutDownPayload };
                        Monitor.Wait(_lock, timeout);
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (inServiceRequests == 0)
                            return new Response { Status = 200, Payload = toShutDownPayload };
                        else
                            return new Response { Status = 500, Payload = serverError };
                    }
                } while (true);
            }
        }
    }
}
