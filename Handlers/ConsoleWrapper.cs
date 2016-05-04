using System;
using Newtonsoft.Json;

namespace DataCenter.Handlers
{
    public class ConsoleWrapper
    {
	    private readonly Module _module;

	    public ConsoleWrapper(Module module)
        {
	        _module = module;
        }

	    // ReSharper disable once InconsistentNaming
	    // ReSharper disable once UnusedMember.Global
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

			_module.Database.Save("log", new {
				module = _module.Name,
				time = DateTime.Now,
				message = param
			});
        }
    }
}