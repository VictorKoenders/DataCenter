using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using DataCenter.API;
using DataCenter.Handlers;
using Jint;
using Jint.Native;

namespace DataCenter
{
    public class Module : IDisposable
    {
	    private dynamic State { get; }
	    private string Directory { get; }
        public string Name { get; }
        public bool Running { get; private set; }
	    private Thread Thread { get; set; }

        public Engine Engine { get; }
        public Database Database { get; }
	    private ConsoleWrapper Console { get; }
	    private TcpConnectionHandler TcpConnectionHandler { get; }
	    private HtmlChecker HtmlChecker { get; }

	    private bool CalledInit { get; set; }

	    public Dictionary<string, List<JsValue>> Events { get; }

	    private dynamic Config { get; }

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

			Config = Database.LoadModuleConfig(Name);

            Engine.SetValue("on", new Action<string, JsValue>(RegisterListener));
			Engine.SetValue("emit", new EmitDelegate(Emit));
            Engine.SetValue("console", Console);
            Engine.SetValue("database", Database);
            Engine.SetValue("state", State);
            Engine.SetValue("config", Config);

	        ApiManager.Instance.Register(this);
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
                    System.Console.WriteLine("[{1:G}] Could not parse {0}:", Name, DateTime.Now);
                    System.Console.WriteLine("#{0}: {1}", ex.LineNumber, ex.Message);
                }
                catch (Jint.Runtime.JavaScriptException ex)
                {
                    System.Console.WriteLine("[{1:G}] Could not execute {0}::{2}:", Name, DateTime.Now, name);
                    System.Console.WriteLine("#{0}: {1}", ex.LineNumber, ex.Message);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("[{1:G}] Could not execute {0}::{2}:", Name, DateTime.Now, name);
                    System.Console.WriteLine(ex.Message);
                }
            }
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

                while (Running)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Jint.Parser.ParserException ex)
            {
                System.Console.WriteLine("[{1:G}] Could not parse {0}:", Name, DateTime.Now);
                System.Console.WriteLine("#{0}: {1}", ex.LineNumber, ex.Message);
            }
            catch (Jint.Runtime.JavaScriptException ex)
            {
                System.Console.WriteLine("[{1:G}] Could not execute {0}:", Name, DateTime.Now);
                System.Console.WriteLine("#{0}: {1}", ex.LineNumber, ex.Message);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[{1:G}] Could not execute {0}:", Name, DateTime.Now);
                System.Console.WriteLine(ex.Message);
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