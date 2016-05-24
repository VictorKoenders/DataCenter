using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DataCenter.Web.Client
{
    public abstract class ClientMessage
    {
        protected readonly TcpClient Client;

        public string HttpVersion { protected get; set; }
        public int StatusCode { protected get; set; } = 200;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        protected ClientMessage(TcpClient client)
        {
            Client = client;
        }
    }
}