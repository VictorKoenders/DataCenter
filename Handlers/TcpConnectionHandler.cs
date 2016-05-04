using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Sockets;
using System.Text;

namespace DataCenter.Handlers
{
    public class TcpConnectionHandler
    {
        private readonly Module _module;

        public readonly List<TcpClientData> Clients;

        public TcpConnectionHandler(Module module)
        {
            _module = module;
            Clients = new List<TcpClientData>();
            
            module.Engine.SetValue("TcpConnection", new Func<string, int, object>(Connect));
        }
        
        private object Connect(string host, int port)
        {
            TcpClientData client = new TcpClientData();
            client.Host = host;
            client.Port = port;
            client.Context = new TcpConnectionContext(this, client);

            Clients.Add(client);

            return client.Context;
        }

        private void Connect(TcpClientData client)
        {
            client.TcpClient = new TcpClient();
            client.TcpClient.Connect(client.Host, client.Port);

            client.Stream = client.TcpClient.GetStream();
            client.Stream.BeginRead(client.ByteBuffer, 0, client.ByteBuffer.Length, TcpCallback, client);

            _module.Emit("tcp_connect", client.Context);
        }

        private void TcpCallback(IAsyncResult ar)
        {
            TcpClientData client = (TcpClientData) ar.AsyncState;
            int bytesRead;
            try
            {
                bytesRead = client.Stream.EndRead(ar);
            }
            catch
            {
                _module.Emit("tcp_disconnect", client.Context);
                if (_module.Running)
                {
                    Utils.SetTimeout(() => Connect(client), 1000);
                }
                return;
            }

            if (bytesRead <= 0)
            {
                _module.Emit("tcp_disconnect", client.Context);
                if (_module.Running)
                {
                    Utils.SetTimeout(() => Connect(client), 1000);
                }
                return;
            }

            client.StringBuffer += Encoding.UTF8.GetString(client.ByteBuffer, 0, bytesRead);
            _module.Emit("tcp_data", client.Context, client.StringBuffer);

            string[] split = client.StringBuffer.Split(new[] {client.Context.seperator}, StringSplitOptions.None);
            for (int i = 0; i < split.Length - 1; i++)
            {
                _module.Emit("tcp_message", client.Context, split[i]);
            }
            client.StringBuffer = split[split.Length - 1];

            client.Stream.BeginRead(client.ByteBuffer, 0, client.ByteBuffer.Length, TcpCallback, client);
        }

        public class TcpConnectionContext
        {
            private readonly TcpConnectionHandler _tcpConnectionHandler;
            private readonly TcpClientData _client;

            public dynamic context;
            public string seperator;

            public TcpConnectionContext(TcpConnectionHandler tcpConnectionHandler, TcpClientData client)
            {
                _tcpConnectionHandler = tcpConnectionHandler;
                _client = client;

                context = new ExpandoObject();
            }

            public void connect()
            {
                _tcpConnectionHandler.Connect(_client);
            }

            public void write(string message)
            {
                message += seperator;
                byte[] data = Encoding.ASCII.GetBytes(message);
                _client.Stream.Write(data, 0, data.Length);
            }
        }

        public class TcpClientData
        {
            public string Host;
            public int Port;
            public TcpClient TcpClient;
            public NetworkStream Stream;
            public TcpConnectionContext Context;
            public readonly byte[] ByteBuffer = new byte[1024];
            public string StringBuffer = "";
        }
    }
}