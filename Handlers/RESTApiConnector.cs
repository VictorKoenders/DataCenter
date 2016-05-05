using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DataCenter.Handlers.OAuth;
using DataCenter.Web.Client;
using Jint.Native;
using Jint.Native.Json;

namespace DataCenter.Handlers
{
    public class RESTApiConnector
    {
        private readonly Module _module;
        private List<RESTContext> _contexts = new List<RESTContext>();

        public RESTApiConnector(Module module)
        {
            _module = module;

            module.Engine.SetValue("APIConnector", new Func<ExpandoObject, RESTContext>(Create));
            module.Engine.SetValue("CreateOAuthRedirect", new Func<string, OAuthRedirectStep>(url => new OAuthRedirectStep(url)));
            module.Engine.SetValue("GetOAuthResponse", new Func<string, OAuthResponseListenerStep>(url => new OAuthResponseListenerStep(url)));
            module.Engine.SetValue("CreateOAuthCompareState", new Func<string, string, OAuthCompareStateStep>((v1, v2) => new OAuthCompareStateStep(v1, v2)));
            module.Engine.SetValue("CreateOAuthPost", new Func<string, ExpandoObject, OAuthHTTPPostStep>((url, arg) => new OAuthHTTPPostStep(url, arg, null)));
            module.Engine.SetValue("CreateOAuthPost", new Func<string, ExpandoObject, string, OAuthHTTPPostStep>((url, arg, format) => new OAuthHTTPPostStep(url, arg, format)));
        }

        private RESTContext Create(ExpandoObject arg)
        {
            RESTContext context = new RESTContext(this, _module);
            IDictionary<string, object> a = arg;
            if (a.ContainsKey("oauth"))
            {
                context.OAuth = new OAuthDefinition(_module, (ExpandoObject) a["oauth"]);
            }
            if (a.ContainsKey("headers"))
            {
                IDictionary<string, object> headers = (IDictionary<string, object>) a["headers"];
                foreach (KeyValuePair<string, object> pair in headers)
                {
                    context.AdditionalHeaders.Add(pair.Key, pair.Value.ToString());
                }
            }
            if (a.ContainsKey("methods"))
            {
                object[] methods = a["methods"] as object[];
                if (methods != null)
                {
                    foreach (IDictionary<string, object> method in methods.Cast<IDictionary<string, object>>())
                    {
                        RESTContext.RESTMethod restMethod = new RESTContext.RESTMethod();
                        foreach (KeyValuePair<string, object> key in method)
                        {
                            if (key.Key == "oauth2_access_token") restMethod.OAuth2AccessToken = key.Value.ToString();
                            if (key.Key == "name") restMethod.Name = key.Value.ToString();
                            if (key.Key == "url") restMethod.Url = key.Value.ToString();
                        }
                        context.Methods.Add(restMethod);
                    }
                }
            }
            return context;
        }

        public void OnApiRequest(ClientRequest request, ClientResponse response)
        {
            foreach (RESTContext context in _contexts)
            {
                if (context.OAuth != null)
                {
                    context.OAuth.OnApiRequest(request, response);
                    if (response.IsFlushed) return;
                }
            }
        }

        public class RESTContext
        {
            private readonly RESTApiConnector _restApiConnector;
            private readonly Module _module;

            public RESTContext(RESTApiConnector restApiConnector, Module module)
            {
                _restApiConnector = restApiConnector;
                _module = module;
                _restApiConnector._contexts.Add(this);
            }

            ~RESTContext()
            {
                _restApiConnector._contexts.Remove(this);
            }

            public void execute(string name, object arguments, JsValue cb)
            {
                var method = Methods.FirstOrDefault(m => m.Name == name);
                string requestBody = null;
                if (arguments != null)
                {
                    if (arguments is string)
                    {
                        requestBody = arguments as string;
                    }
                    else
                    {
                        requestBody = string.Join("&", ((ExpandoObject)arguments).Select(a => a.Key + "=" + a.Value));
                    }
                }
                if (method == null)
                {
                    _module.Console.log("Could not find REST method", name);
                    return;
                }
                WebRequest request = WebRequest.Create(method.Url + (method.Method == "GET" ? "?" + requestBody : ""));
                foreach (KeyValuePair<string, string> header in AdditionalHeaders)
                {
                    if (header.Key == "Content-Type")
                    {
                        request.ContentType = header.Value;
                    }
                    else
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
                if (!string.IsNullOrEmpty(method.OAuth2AccessToken))
                {
                    string authHeader = method.OAuth2AccessToken.Replace("{auth.access_token}", _module.Config.auth.access_token);
                    authHeader = authHeader.Replace("{get_params}", requestBody != null ? "?" + requestBody : "");
                    request.Headers.Add("Authorization", authHeader);
                }
                if (method.Method != "GET" && requestBody != null)
                {
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(Encoding.ASCII.GetBytes(requestBody), 0, requestBody.Length);
                    }
                }

                Task<WebResponse> task = request.GetResponseAsync();
                task.ContinueWith(r =>
                {
                    try
                    {
                        var response = r.Result;
                        string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        cb.Invoke(JsValue.Null, new[] {new JsonParser(_module.Engine).Parse(body)});
                    }
                    catch (AggregateException ex)
                    {
                        var response = ((WebException)ex.InnerExceptions[0]).Response;
                        string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    }
                    catch (WebException ex)
                    {
                        var response = ex.Response;
                        string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    }
                    catch(Exception ex)
                    {
                        
                    }
                });
            }

            public void execute(string name, JsValue cb)
            {
                execute(name, null, cb);
            }

            public OAuthDefinition OAuth { get; set; }
            public Dictionary<string, string> AdditionalHeaders { get; } = new Dictionary<string, string>();
            public List<RESTMethod> Methods { get; } = new List<RESTMethod>();

            public class RESTMethod
            {
                public string Name { get; set; }
                public string OAuth2AccessToken { get; set; }
                public string Url { get; set; }
                public string Method { get; set; } = "GET";
            }
        }
    }
}
