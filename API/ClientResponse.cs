using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace DataCenter.API
{
	public abstract class ClientMessage
	{
		protected readonly TcpClient Client;

		public string HttpVersion { protected get; set; }
		public int StatusCode { protected get; set; } = 200;
		public Dictionary<string, string> Headers { protected get; set; } = new Dictionary<string, string>();

		protected ClientMessage(TcpClient client)
		{
			Client = client;
		}
	}

	public class ClientResponse : ClientMessage
	{
		public object Body { private get; set; }

		public ClientResponse(TcpClient client) : base(client)
		{
		}

		private void SetHeader(string key, string value)
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

		public void Flush()
		{
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
			stream.Close();
		}
	}

	public class ClientRequest : ClientMessage
	{
		public ClientRequest(TcpClient client) : base(client)
		{
		}

		public ClientResponse GetResponse()
		{
			return new ClientResponse(Client) { HttpVersion = HttpVersion };
		}

		public string Url { get; set; }
		public string Body { private get; set; }
		public string Method { get; set; }
	}
}