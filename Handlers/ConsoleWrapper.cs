using System;
using Newtonsoft.Json;

namespace DataCenter.Handlers
{
    public class ConsoleWrapper
    {
        private Database _database;

        public ConsoleWrapper(Database database)
        {
            _database = database;
        }

        public void log(params object[] param)
        {
            bool first = true;
            foreach (object o in param)
            {
                if (first) first = false;
                else Console.Write(" ");
                if (o == null)
                {
                    Console.WriteLine("null");
                    continue;
                }
                Type type = o.GetType();
                if (type.IsValueType || o is string)
                {
                    Console.Write(o);
                }
                else
                {
                    Console.Write(JsonConvert.SerializeObject(o));
                }
            }
            Console.WriteLine();
        }
    }
}