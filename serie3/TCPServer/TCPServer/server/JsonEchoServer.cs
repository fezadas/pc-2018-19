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
using TCPServer.service;
using TCPServer;

namespace JsonEchoServer
{
    // CAUTION: does not support proper shutdown
    
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

        private static JObject nonExistentQueuePayload,successPayload,
            timeoutPayload,formatErrorPayload, toShutDownPayload;

        private static ConcurrentDictionary<string, MessageQueue> messageCollections;
        private volatile static int pendingOperations;
        private volatile static bool toShutDown;

        static async Task Main(string[] args)
        {   
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            Console.WriteLine($"Listening on {port}");

            messageCollections = new ConcurrentDictionary<string, MessageQueue>();
        
            nonExistentQueuePayload = new JObject(new JProperty("body","Fila não existente"));
            successPayload = new JObject(new JProperty("body","Sucesso"));
            formatErrorPayload = new JObject(new JProperty("body","Formato inválido"));
            timeoutPayload = new JObject(new JProperty("body","Timeout"));
            toShutDownPayload = new JObject(new JProperty("body", "Shutdown"));

            //Services services = 

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
                            case "SHUTDOWN": response = await ShutDown(); break;
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

            if(toShutDown) return new Response { Status = 503, Payload = toShutDownPayload };

            messageCollections.TryGetValue(path, out MessageQueue mQueue);
            if(mQueue == null)
            {
                messageCollections.TryAdd(path, new MessageQueue(10));
            }

            return new Response { Status = 200 , Payload= successPayload };
        }

        public async static Task<Response> Send(Request request) {

            if (toShutDown) return new Response { Status = 503, Payload = toShutDownPayload };

            var payload = request.Payload;

            messageCollections.TryGetValue(request.Path, out MessageQueue mQueue);
            if (mQueue == null)
            {
                return new Response { Status = 404 , Payload = nonExistentQueuePayload };
            }

            Interlocked.Increment(ref pendingOperations);

            await mQueue.PutAsync(new Message(payload));

            return new Response { Status = 200 , Payload = successPayload };
        }

        public async static Task<Response> Receive(Request request) {

            if (toShutDown) return new Response { Status = 503, Payload = toShutDownPayload };

            String tHeader;
            int timeout;
            request.Headers.TryGetValue("timeout", out tHeader);

            if (tHeader == null) { timeout = Timeout.Infinite; }
            else int.TryParse(tHeader, out timeout);

            messageCollections.TryGetValue(request.Path, out MessageQueue mQueue);
            if (mQueue == null)
            {
                return new Response { Status = 404 , Payload = nonExistentQueuePayload };
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            Message message = await mQueue.TakeAsync(timeout, ct);

            if(message == null)
            {
                return new Response { Status = 204 , Payload = timeoutPayload };
            }

            return new Response { Status = 200 , Payload = message.Payload };

        }

        public async static Task<Response> ShutDown() {

            if (toShutDown) return new Response { Status = 503, Payload = toShutDownPayload };

            toShutDown = true;
        }

    }

}
