using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DataCenter.Web.Client
{
    public class ClientSocket : ClientMessage
    {
        public event Action<ClientSocket, string> Message;

        private readonly byte[] buffer = new byte[1024];
        private readonly List<byte> byteBuffer = new List<byte>();

        public ClientSocket(TcpClient client, ClientMessage request, ClientResponse response) : base(client)
        {
            string key = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(request.Headers["Sec-WebSocket-Key"] + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

            response.StatusCode = 101;
            response.Headers.Add("Connection", "Upgrade");
            response.Headers.Add("Upgrade", "websocket");
            response.Headers.Add("Sec-WebSocket-Accept", key);

            response.Flush(false);

            client.GetStream().BeginRead(buffer, 0, 1024, Callback, null);

            ApiManager.Instance.Register(this);
        }

        ~ClientSocket()
        {
            ApiManager.Instance.Remove(this);
        }

        private void Callback(IAsyncResult ar)
        {
            int bytesRead = Client.GetStream().EndRead(ar);
            if (bytesRead > 0)
            {
                byteBuffer.AddRange(buffer.Take(bytesRead));

                ParseByteBuffer();
                
                Client.GetStream().BeginRead(buffer, 0, 1024, Callback, null);
            }
        }

        private void ParseByteBuffer()
        {
            if (byteBuffer.Count < 6) return;
            
            int length;
            int offset;

            if (byteBuffer[1] - 128 < 126)
            {
                length = byteBuffer[1] - 128;
                offset = 2;
            }
            else if (byteBuffer[1] - 128 == 126)
            {
                length = BitConverter.ToUInt16(byteBuffer.Take(4).Reverse().ToArray(), 0);
                offset = 4;
            }
            else if (byteBuffer[1] - 128 == 127)
            {
                length = (int)BitConverter.ToUInt64(byteBuffer.Take(10).Reverse().ToArray(), 0);
                offset = 10;
            }
            else
            {
                throw new Exception();
            }
            if (byteBuffer.Count < length + offset) return;

            byte[] keys = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                keys[i] = byteBuffer[i + offset];
            }

            byte[] data = new byte[length];

            for (int i = 0; i < length; i++)
            {
                data[i] = (byte) (byteBuffer[i + offset + 4] ^ keys[i%4]);
            }
            byteBuffer.RemoveRange(0, length + offset + 4);

            Message?.Invoke(this, Encoding.ASCII.GetString(data));
        }

        public void Write(object data)
        {
            List<byte> buffer = new List<byte>();
            buffer.Add(129);
            byte[] bytes;
            if (!(data is string)) data = JsonConvert.SerializeObject(data);
            bytes = Encoding.ASCII.GetBytes((string) data);
            if (bytes.Length < 126) buffer.Add((byte) (bytes.Length));

            else
            {
                buffer.Add(126);
                buffer.AddRange(BitConverter.GetBytes((short)bytes.Length).Reverse());
            }
            buffer.AddRange(bytes);
            Client.GetStream().Write(buffer.ToArray(), 0, buffer.Count);
        }
    }
}