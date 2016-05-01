using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using DataCenter.Handlers;
using Jint;
using Jint.Native;

namespace DataCenter
{
    public class Module : IDisposable
    {
        public dynamic State { get; set; }
        public string Directory { get; set; }
        public string Name { get; set; }
        public bool Running { get; set; }
        public Thread Thread { get; set; }

        public Engine Engine { get; set; }
        public Database Database { get; set; }
        public ConsoleWrapper Console { get; set; }
        public TcpConnectionHandler TcpConnectionHandler { get; set; }

        public bool CalledInit { get; set; }

        public Dictionary<string, List<JsValue>> events { get; set; }

        public dynamic Config { get; set; }

        public Module(string name, string dir)
        {
            Name = name;
            Directory = dir;
            Running = true;
            State = new ExpandoObject();
            
            Engine = new Engine();
            Database = new Database(Engine);
            Console = new ConsoleWrapper(Database);
            events = new Dictionary<string, List<JsValue>>();
            TcpConnectionHandler = new TcpConnectionHandler(this);

            Config = Database.LoadModuleConfig(Name);

            Engine.SetValue("on", new Action<string, JsValue>(RegisterListener));
            Engine.SetValue("console", Console);
            Engine.SetValue("database", Database);
            Engine.SetValue("state", State);
            Engine.SetValue("config", Config);

            Engine.SetValue("TcpClient", TcpConnectionHandler);
        }

        private void RegisterListener(string name, JsValue cb)
        {
            if(!events.ContainsKey(name)) events.Add(name, new List<JsValue>());
            events[name].Add(cb);
        }

        public void Emit(string name, object context, params object[] value)
        {
            if (!events.ContainsKey(name)) return;

            foreach (JsValue ev in events[name])
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
            events.Clear();
        }

        public void Dispose()
        {
            Interrupt();
            foreach (TcpConnectionHandler.TcpClientData client in TcpConnectionHandler.Clients)
            {
                client.TcpClient.Close();
            }
        }
    }
}