using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DataCenter.Web.Client
{
	public class ClientListener
	{
		private readonly ApiManager _manager;
		private readonly TcpClient _client;

		private readonly StringBuilder _stringBuffer = new StringBuilder();
		private readonly byte[] _buffer = new byte[1024];

		private Dictionary<string, string> _headers;
		private string _body;
		private string _header;

		private DateTime startTime;

		public ClientListener(ApiManager manager, TcpClient client)
		{
			_manager = manager;
			_client = client;
			startTime = DateTime.Now;

			_client.GetStream().BeginRead(_buffer, 0, _buffer.Length, DataReceived, null);
		}

		private void DataReceived(IAsyncResult ar)
		{
			int bytesRead = _client.GetStream().EndRead(ar);
			if (bytesRead > 0)
			{
				_stringBuffer.Append(Encoding.ASCII.GetString(_buffer, 0, bytesRead));
				ParseHeaders();
				if (Done())
				{
					HandleRequest();
					return;
				}
				_client.GetStream().BeginRead(_buffer, 0, _buffer.Length, DataReceived, null);
				return;
			}

			if (_stringBuffer.Length > 0)
			{
				HandleRequest();
			}
		}

		private bool Done()
		{
			if (_headers == null) return false;
			if (!_headers.ContainsKey("Content-Length")) return true;

			int length;
			if (!int.TryParse(_headers["Content-Length"], out length))
			{
				return false;
			}
			return length == _body.Length;
		}

		private void ParseHeaders()
		{
			_headers = null;
			string str = _stringBuffer.ToString();
			int index = str.IndexOf("\r\n\r\n", StringComparison.Ordinal);
			if (index == -1)
			{
				return;
			}
			_body = str.Substring(index + 4);

			string[] split = str.Substring(0, index).Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);

			_headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_header = split[0];
			for (int i = 1; i < split.Length; i++)
			{
				string line = split[i];
				index = line.IndexOf(':');
				if (index == -1)
				{
					continue;
				}
				string key = line.Substring(0, index);
				string value = line.Substring(index + 1).Trim();

				if (_headers.ContainsKey(key))
				{
					_headers[key] += ";" + value;
				}
				else
				{
					_headers.Add(key, value);
				}
			}
		}

		private void HandleRequest()
		{
			string[] split = _header.Split(' ');

			ClientRequest request = new ClientRequest(_client)
			{
				Body = _body,
				Headers = _headers,
				Method = split[0],
				Url = split[1],
				HttpVersion = split[2]
			};

			Console.WriteLine("Received data for {1} in {0} ms", (DateTime.Now - startTime).TotalMilliseconds, request.Url);
			_manager.HandleRequest(request);
		}
	}
}