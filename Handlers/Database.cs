using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jint.Native;
using Jint.Native.Json;
using Newtonsoft.Json;

namespace DataCenter.Handlers
{
    public class Database
    {
        private readonly List<string> _verifiedDBNames = new List<string>();
        private readonly Module _module;

        public Database(Module module)
        {
			_module = module;
        }

        public void Save(string db, object o)
        {
            Save(db, o, JsValue.Null);
        }

        public void Save(string db, object o, JsValue callback)
        {
            VerifyDatabase(db);
            Task<string> task = ExecuteAsync("/" + db, RequestType.Post, o);

            if (callback.IsNull()) return;

            task.ContinueWith(result =>
            {
                JsValue value = JsValue.Null;
                if (!string.IsNullOrEmpty(result.Result))
                {
                    try
                    {
                        value = new JsonParser(_module.Engine).Parse(result.Result);
                    }
                    catch
                    {
                        value = JsValue.Null;
                    }
                }
                callback.Invoke(value);
            });
        }

        private void VerifyDatabase(string name)
        {
	        if (_verifiedDBNames.Contains(name)) return;

	        DBStatus result = Execute<DBStatus>("/" + name, RequestType.Get);
	        if (result == null)
	        {
		        // no db connection
		        return;
	        }
	        if (result.Error == "not_found")
	        {
		        Execute("/" + name, RequestType.Put);
	        }
	        _verifiedDBNames.Add(name);
        }

        private class DBStatus
        {
            [JsonProperty("error")]
            public string Error { get; set; }
            [JsonProperty("reason")]
            public string Reason { get; set; }
        }

        private enum RequestType
        {
            Get,
            Put,
            Post
        }

        private async Task<string> ExecuteAsync(string url, RequestType requestType, object body = null)
        {
            HttpWebRequest request = WebRequest.CreateHttp("http://localhost:5984" + url);

            switch (requestType)
            {
                case RequestType.Get:
                    request.Method = "GET";
                    break;
                case RequestType.Put:
                    request.Method = "PUT";
                    break;
                case RequestType.Post:
                    request.Method = "POST";
                    break;
                default:
                    throw new NotImplementedException();
            }

            request.Accept = "application/json";
            request.ContentType = "application/json";
	        Stream stream;
            if (body != null)
            {
                string bodyString = JsonConvert.SerializeObject(body);
                byte[] bytes = Encoding.ASCII.GetBytes(bodyString);
                request.ContentLength = bytes.Length;

                stream = request.GetRequestStream();
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
            HttpWebResponse response;
            try
            {
                response = await request.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
			}
	        if (response == null)
	        {
		        return null;
	        }
			stream = response.GetResponseStream();
			if (stream == null)
			{
				return null;
			}
			string result = await new StreamReader(stream).ReadToEndAsync();
            if (url != "/log")
            {
                Log(url, requestType, body, request, response, result);
            }
            return result;
        }
        private string Execute(string url, RequestType requestType, object body = null)
        {
            HttpWebRequest request = WebRequest.CreateHttp("http://localhost:5984" + url);

            switch (requestType)
            {
                case RequestType.Get:
                    request.Method = "GET";
                    break;
                case RequestType.Put:
                    request.Method = "PUT";
                    break;
                case RequestType.Post:
                    request.Method = "POST";
                    break;
                default:
                    throw new NotImplementedException();
            }

            request.Accept = "application/json";
            request.ContentType = "application/json";
	        Stream stream;
	        if (body != null)
            {
                string bodyString = JsonConvert.SerializeObject(body);
                byte[] bytes = Encoding.ASCII.GetBytes(bodyString);
                request.ContentLength = bytes.Length;

	            try
	            {
		            stream = request.GetRequestStream();
		            stream.Write(bytes, 0, bytes.Length);
	            }
	            catch
	            {
		            return null;
	            }
            }
            HttpWebResponse response;
            try
            {
                response = request.GetResponse() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
            }
	        if (response == null)
	        {
		        return "";
	        }
	        stream = response.GetResponseStream();
	        if (stream == null)
	        {
		        return "";
	        }

			string result = new StreamReader(stream).ReadToEnd();
            if (url != "/log")
            {
                Log(url, requestType, body, request, response, result);
            }
            return result;
        }
        private T Execute<T>(string url, RequestType requestType, object body = null)
        {
	        string result = Execute(url, requestType, body);

	        return string.IsNullOrEmpty(result) 
				? default(T) 
				: JsonConvert.DeserializeObject<T>(result);
        }

        private async void Log(string url, RequestType requestType, object body, WebRequest request, HttpWebResponse response, string responseBody)
        {
            VerifyDatabase("log");
            Dictionary<string, string> responseHeaders = new Dictionary<string, string>();
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
            foreach (string key in response.Headers.Keys)
            {
                if (responseHeaders.ContainsKey(key))
                {
                    responseHeaders[key] += response.Headers[key];
                }
                else
                {
                    responseHeaders.Add(key, response.Headers[key]);
                }
            }
            foreach (string key in request.Headers.Keys)
            {
                if (requestHeaders.ContainsKey(key))
                {
                    requestHeaders[key] += request.Headers[key];
                }
                else
                {
                    requestHeaders.Add(key, request.Headers[key]);
                }
            }
            
            await ExecuteAsync("/log", RequestType.Post, new
            {
                url = url,
                requestType = requestType.ToString(),
                request = new
                {
                    headers = requestHeaders,
                    body = body
                },
                response = new
                {
                    code = (int)response.StatusCode,
                    headers = responseHeaders,
                    body = responseBody
                }
            });
        }

        public void SaveModuleConfig(string name, dynamic obj)
        {
            obj._id = name;
            Execute("/module_config/" + name, RequestType.Put, obj);
        }

        public dynamic LoadModuleConfig(string name)
        {
            VerifyDatabase("module_config");
            string result = Execute("/module_config/" + name, RequestType.Get);
            dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
            if (value == null || (((IDictionary<string, object>)value).ContainsKey("error") && value.error == "not_found"))
            {
                return new ExpandoObject();
            }
            return value;
        }
    }
}