/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
*/

using System;
using System.Dynamic;
using DataCenter;

namespace Test
{
	internal static class Program
	{
		private static void Main()
        {
            string format = "hello {planet}";

		    dynamic arg = new ExpandoObject();
		    arg.planet = "world";
		    string resultString = Utils.FormatString(format, arg);
		    if (resultString != "hello world")
		    {
		        throw new Exception();
		    }
		    if (!Utils.FormatMatches(format, resultString))
		    {
		        throw new Exception();
		    }
            dynamic result = Utils.ParseFormatString(format, resultString);
		    if (result.planet != "world")
		    {
		        throw new Exception();
		    }

            string key1 = "/authorize/linkedin?code=AQReuarT2TTImThUJW5tKwPN91LV01mMSC-tmL25el-b80hUEfQJaZS_3AiPccs8bFcv3LHvbWYEjWPrZacr44bNyvwQJTi5N2-W-KzOU4Ufmh7xRrQ&state=203TOQNBE3PRXE73QMVV3";
		    string key2 = "/authorize/linkedin?code={code}&state={response_code}";


            if (!Utils.FormatMatches(key2, key1))
            {
                throw new Exception();
            }
        }
	}
}
