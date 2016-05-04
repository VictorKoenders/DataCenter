using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Jint.Native;

namespace DataCenter.API
{
	public class ApiManager
	{
		private readonly Mutex _moduleMutex = new Mutex();
		private readonly List<Module> _modules  = new List<Module>();
		private TcpListener listener;

		private ApiManager()
		{
			
		}

		public void Start()
		{
			listener = new TcpListener(IPAddress.Any, 80);
			listener.Start();
			listener.BeginAcceptTcpClient(SocketAccepted, null);
		}

		private void SocketAccepted(IAsyncResult ar)
		{
			try
			{
				new ClientListener(this, listener.EndAcceptTcpClient(ar));
			}
			catch
			{
				
			}
			finally
			{
				if (listener != null)
				{
					listener.BeginAcceptTcpClient(SocketAccepted, null);
				}
			}
		}

		public void Stop()
		{
			listener.Stop();
			listener = null;
		}

		public void Register(Module module)
		{
			_moduleMutex.WaitOne();
			_modules.Add(module);
			_moduleMutex.ReleaseMutex();
		}

		public void Remove(Module module)
		{
			_moduleMutex.WaitOne();
			_modules.Remove(module);
			_moduleMutex.ReleaseMutex();
		}

		public static ApiManager Instance { get; } = new ApiManager();

		public void HandleRequest(ClientRequest request)
		{
			ClientResponse response = request.GetResponse();
			try
			{
				if (request.Url.StartsWith("/api/"))
				{
					// TODO: Implement API request
				}
				else
				{
					string url = request.Url;
					if (url == "/") url = "/index.html";
					url = "public/" + url.Substring(1);
					if (!File.Exists(url))
					{
						response.StatusCode = 404;
					}
					else
					{
						response.Body = File.ReadAllText(url);
					}
				}
			}
			catch (Exception ex)
			{
				response.StatusCode = 500;
				response.Body = ex.Message;
			}
			response.Flush();
		}
	}
}