using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using DataCenter.Handlers;
using DataCenter.Web;
using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Native.Object;
using Newtonsoft.Json;
using JsonSerializer = Jint.Native.Json.JsonSerializer;

namespace DataCenter
{
    public class Module : IDisposable
    {
        public dynamic State { get; }
	    private string Directory { get; }
        public string Name { get; }
        public bool Running { get; private set; }
	    private Thread Thread { get; set; }
		private string _lastStateJSON = string.Empty;

		public Engine Engine { get; }
        public Database Database { get; }
        public ConsoleWrapper Console { get; }
	    private TcpConnectionHandler TcpConnectionHandler { get; }
        public RESTApiConnector RESTApiConnector { get; }
        private HtmlChecker HtmlChecker { get; }

	    private bool CalledInit { get; set; }

	    public Dictionary<string, List<JsValue>> Events { get; }

        public dynamic Config { get; }

        public Module(string name, string dir)
        {
            Name = name;
            Directory = dir;
            Running = true;
            State = new ExpandoObject();
            
            Engine = new Engine();
            Database = new Database(this);
            Console = new ConsoleWrapper(this);
            Events = new Dictionary<string, List<JsValue>>();
            TcpConnectionHandler = new TcpConnectionHandler(this);
			HtmlChecker = new HtmlChecker(this);
            RESTApiConnector = new RESTApiConnector(this);

            Config = Database.LoadModuleConfig(Name);

            Engine.SetValue("on", new Action<string, JsValue>(RegisterListener));
			Engine.SetValue("emit", new EmitDelegate(Emit));
            Engine.SetValue("console", Console);
            Engine.SetValue("database", Database);
            Engine.SetValue("state", State);
            Engine.SetValue("config", Config);
	        Engine.SetValue("update_state", new Action(UpdateState));

            Engine.SetValue("randomString", new Func<int, string>(length =>
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                Random random = new Random();
                return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            }));

	        UpdateState();

	        ApiManager.Instance.Register(this);
        }

	    public void UpdateState()
	    {
		    string stateJSON = JsonConvert.SerializeObject(State);
		    if (stateJSON == _lastStateJSON) return;
		    ApiManager.Instance.EmitStateChange(this);
		    _lastStateJSON = stateJSON;
	    }

	    private delegate void EmitDelegate(string name, object context, params object[] value);

        private void RegisterListener(string name, JsValue cb)
        {
            if(!Events.ContainsKey(name)) Events.Add(name, new List<JsValue>());
            Events[name].Add(cb);
        }

        public void Emit(string name, object context, params object[] value)
        {
            if (!Events.ContainsKey(name)) return;

            foreach (JsValue ev in Events[name])
            {
                try
                {
                    ev.Invoke(JsValue.FromObject(Engine, context), value.Select(v => JsValue.FromObject(Engine, v)).ToArray());
                }
                catch (Jint.Parser.ParserException ex)
                {
                    Console.log("Could not parse", Name, ex.LineNumber, ex.Message);
                }
                catch (Jint.Runtime.JavaScriptException ex)
				{
					Console.log("Could not execute", Name, "::", name, ex.LineNumber, ex.Message);
                }
                catch (Exception ex)
				{
					Console.log("Could not execute", Name, "::", name, ex.Message);
                }
            }
			UpdateState();
        }
        
        public void Start()
        {
            Running = true;
            Thread = new Thread(Entry);
            Thread.Start();
        }

        private void Entry()
        {
            try
            {
                string contents = File.ReadAllText(Path.Combine(Directory, "index.js"));

                Engine.Execute(contents);
                Engine.Invoke(!CalledInit ? "init" : "update");
                CalledInit = true;
				UpdateState();

				while (Running)
                {
                    Thread.Sleep(100);
                }
			}
			catch (Jint.Parser.ParserException ex)
			{
				Console.log("Could not parse", Name, ex.LineNumber, ex.Message);
			}
			catch (Jint.Runtime.JavaScriptException ex)
			{
				Console.log("Could not execute", Name, ex.Location.Start.Line + "::"+ ex.Location.Start.Column, ex.Message);
			}
			catch (Exception ex)
			{
				Console.log("Could not execute", Name, ex.Message);
			}
			Database.SaveModuleConfig(Name, Config);
		}

        public void Interrupt()
        {
            Running = false;
            Events.Clear();
        }

        public void Dispose()
        {
            Interrupt();
            foreach (TcpConnectionHandler.TcpClientData client in TcpConnectionHandler.Clients)
            {
                client.TcpClient.Close();
			}
	        HtmlChecker.Dispose();
			ApiManager.Instance.Remove(this);
		}
    }
}