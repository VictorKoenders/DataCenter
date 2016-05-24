using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DataCenter.Handlers;
using DataCenter.Web.Client;
using Jint.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataCenter.Web
{
    public class ApiManager
    {
        private readonly Mutex _moduleMutex = new Mutex();
        private readonly List<Module> _modules = new List<Module>();
        private readonly Mutex _connectedSocketsMutex = new Mutex();
        private readonly List<ClientSocket> _connectedSockets = new List<ClientSocket>();
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
	            listener?.BeginAcceptTcpClient(SocketAccepted, null);
            }
        }

        public void Stop()
        {
            listener.Stop();
            listener = null;
        }

        private static object GetModuleData(Module module)
        {
            return new
            {
                name = module.Name,
                state = module.State,
                registeredEvents = module.Events.Select(e => e.Key)
            };
        }

        public void Register(Module module)
        {
            _moduleMutex.WaitOne();
            _modules.Add(module);
            _moduleMutex.ReleaseMutex();

            _connectedSocketsMutex.WaitOne();
            foreach (ClientSocket socket in _connectedSockets)
            {
                socket.Write(new { action = "module_loaded", module = GetModuleData(module) });
            }
            _connectedSocketsMutex.ReleaseMutex();
        }

        public void Remove(Module module)
        {
            _moduleMutex.WaitOne();
            _modules.Remove(module);
            _moduleMutex.ReleaseMutex();

            _connectedSocketsMutex.WaitOne();
            foreach (ClientSocket socket in _connectedSockets)
            {
                socket.Write(new { action = "module_unloaded", module = GetModuleData(module) });
            }
            _connectedSocketsMutex.ReleaseMutex();
        }

        public void Register(ClientSocket socket)
        {
            _connectedSocketsMutex.WaitOne();
            _connectedSockets.Add(socket);
            _connectedSocketsMutex.ReleaseMutex();
        }

        public void Remove(ClientSocket socket)
        {
            _connectedSocketsMutex.WaitOne();
            _connectedSockets.Remove(socket);
            _connectedSocketsMutex.ReleaseMutex();
        }

        public static ApiManager Instance { get; } = new ApiManager();

        public void HandleSocketMessage(ClientSocket response, string message)
        {
            try
            {
                JObject o = (JObject)JsonConvert.DeserializeObject(message);

                JToken action = o["action"];
                if (action == null)
                {
                    response.Write(new { error = "action_not_set" });
                    return;
                }
                switch (action.Value<string>())
                {
                    case "load_modules":
                        response.Write(new {
                            action = "load_modules_response",
                            modules = _modules.Select(GetModuleData)
                        });
                        break;
                    case "emit":
                        string moduleName = o["contents"]["module"]?.Value<string>();
                        string methodName = o["contents"]["event"]?.Value<string>();
                        JToken context = o["contents"]["context"];
                        JToken arguments = o["contents"]["arguments"];
                        if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(methodName))
                        {
                            response.Write(new
                            {
                                action = "emit_response",
                                error = "missing arguments"
                            });
                            return;
                        }
                        Module module = _modules.FirstOrDefault(m => m.Name == moduleName);
                        if (module == null || !module.Events.ContainsKey(methodName))
                        {
                            response.Write(new
                            {
                                action = "emit_response",
                                error = "module_or_event_not_found"
                            });
                            return;
                        }

		                var values = new List<object>();
                        foreach (JsValue ev in module.Events[methodName])
                        {
	                        var parser = new Jint.Native.Json.JsonParser(module.Engine);

							var realContext = parser.Parse(JsonConvert.SerializeObject(context));
	                        var realArguments = new JsValue[0];
	                        if (arguments != null)
	                        {
		                        if (arguments.Type == JTokenType.Array)
		                        {
			                        realArguments = new JsValue[arguments.Count()];
			                        int index = 0;
			                        foreach (var arg in arguments)
			                        {
				                        realArguments[index++] = parser.Parse(JsonConvert.SerializeObject(arg));
			                        }
		                        }
		                        else
		                        {
			                        realArguments = new JsValue[1]
			                        {
										parser.Parse(JsonConvert.SerializeObject(arguments))
									};
		                        }
	                        }

							var value = ev.Invoke(realContext, realArguments);
	                        var result = new Jint.Native.Json.JsonSerializer(module.Engine).Serialize(value, JsValue.Null,
		                        JsValue.Null);
	                        if (!result.IsString()) continue;
							var str = result.AsString();
	                        values.Add(JsonConvert.DeserializeObject(str));

                        }
						response.Write(new
						{
							action = "emit_response",
							result = values,
							module = GetModuleData(module)
						});
		                module.UpdateState();

		                break;
                    default:
                        response.Write(new { error = "action_not_found" });
                        break;
                }
            }
            catch
            {
                response.Write(new { error = "invalid_json" });
            }
        }

        public void HandleRequest(ClientRequest request)
        {
			DateTime start = DateTime.Now;
            ClientResponse response = request.GetResponse();
            try
            {
                if (request.Url.StartsWith("/authorize/"))
                {
                    foreach (Module module in _modules)
                    {
                        module.RESTApiConnector.OnApiRequest(request, response);
                        if (response.IsFlushed) return;
                    }
                }
                if (request.Headers.ContainsKey("Upgrade") && request.Headers.ContainsKey("Sec-WebSocket-Key") && request.Headers["Upgrade"] == "websocket")
                {
                    response.Upgrade().Message += HandleSocketMessage;
                    return;
                }
                if (request.Url.StartsWith("/api/"))
                {
	                response.Body = _modules[0].Database.Execute("/error/_design/view/_view/all", Database.RequestType.Get);
	                // TODO: Implement API request
                }
                else
                {
                    string url = request.Url;
                    if (url == "/") url = "/index.html";
					response.File = "public/" + url.Substring(1);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Body = ex.Message;
            }
            response.Flush();
			Console.WriteLine("Created response and flushed for {1} in {0} ms", (DateTime.Now - start).TotalMilliseconds, request.Url);
        }

	    public void EmitStateChange(Module module)
		{
			_connectedSocketsMutex.WaitOne();
			foreach (ClientSocket socket in _connectedSockets)
			{
				socket.Write(new { action = "state_changed", module = GetModuleData(module) });
			}
			_connectedSocketsMutex.ReleaseMutex();
		}
    }
}