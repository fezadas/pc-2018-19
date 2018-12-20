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

        private static ConcurrentDictionary<string, MessageQueue> messageCollections;

        static async Task Main(string[] args)
        {   
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            Console.WriteLine($"Listening on {port}");

            messageCollections = new ConcurrentDictionary<string, MessageQueue>();
            Services services = 

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

                        switch (request.Method)
                        {
                            case "CREATE": Create(request.Path); break;
                            case "SEND": Send(request.Path); break;
                            case "RECEIVE": Receive(request.Path); break;
                            case "SHUTDOWN": ShutDown(); break;
                            default: break;

                        }

                   
                        var response = new Response
                        {
                            Status = 200,
                            Payload = json,
                        };
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
        public static void Create(string path)
        {
            messageCollections.TryGetValue(path, out MessageQueue mQueue);
            if(mQueue == null)
            {
                messageCollections.TryAdd(path,)
            }
        }

        public static void Send(string path) { }

        public static void Receive(string path) { }

        public static void ShutDown() { }

    }

}
