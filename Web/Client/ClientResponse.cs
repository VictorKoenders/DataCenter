using System.Linq;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace DataCenter.Web.Client
{
    public class ClientResponse : ClientMessage
	{
	    private readonly ClientRequest _request;
	    public object Body { private get; set; }

		public ClientResponse(TcpClient client, ClientRequest request) : base(client)
		{
		    _request = request;
		    IsFlushed = false;

		}

	    public void SetHeader(string key, string value)
		{
			if (!Headers.ContainsKey(key))
			{
				Headers.Add(key, value);
			}
			else
			{
				Headers[key] = value;
			}
		}

        public bool IsFlushed { get; set; }

		public void Flush(bool endStream = true)
		{
		    if (IsFlushed) return;
		    IsFlushed = true;

            string responseBody = "";
			if (Body != null)
			{
				responseBody = Body as string ?? JsonConvert.SerializeObject(Body);

				SetHeader("Content-Length", responseBody.Length.ToString());
			}

			string response = HttpVersion + " " + StatusCode + "\r\n" +
				Headers.Aggregate("", (current, header) => current + header.Key + ": " + header.Value + "\r\n") + "\r\n" +
				responseBody;

			byte[] bytes = Encoding.ASCII.GetBytes(response);

			NetworkStream stream = Client.GetStream();
			stream.Write(bytes, 0, bytes.Length);
			stream.Flush();
		    if (endStream) stream.Close();
		}

	    public ClientSocket Upgrade()
	    {
            return new ClientSocket(Client, _request, this);
	    }
	}
}