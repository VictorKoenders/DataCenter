using System.Net.Sockets;

namespace DataCenter.Web.Client
{
    public class ClientRequest : ClientMessage
    {
        public ClientRequest(TcpClient client) : base(client)
        {
        }

        public ClientResponse GetResponse()
        {
            return new ClientResponse(Client, this) { HttpVersion = HttpVersion };
        }

        public string Url { get; set; }
        public string Body { private get; set; }
        public string Method { get; set; }
    }
}