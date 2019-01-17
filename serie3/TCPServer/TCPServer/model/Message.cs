using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer
{
    public class Message
    {
        public JObject Payload { get; set; }

        public Message(JObject payload)
        {
            Payload = payload;
        }
    }
}
