using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace DataCenter.Handlers.OAuth
{
    public class OAuthHTTPPostStep : IOAuthStep
    {
        private readonly string _url;
        private readonly ExpandoObject _data;
        private readonly string _responseFormat;
        private bool succeeded;

        public OAuthHTTPPostStep(string url, ExpandoObject data, string responseFormat)
        {
            _url = url;
            _data = data;
            _responseFormat = responseFormat;
            succeeded = false;
        }

        public void Execute(Module module, OAuthDefinition definition)
        {
            WebRequest request = WebRequest.Create(_url);
            request.Method = "POST";

            IDictionary<string, object> data = _data;
            string content = string.Join("&", data.Select(d => d.Key + "=" + Utils.FormatString(d.Value.ToString(), definition.State)));
            request.ContentLength = content.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(Encoding.ASCII.GetBytes(content), 0, content.Length);
            }
            try
            {
                WebResponse response = request.GetResponse();
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                IDictionary<string, object> result;
                if (string.IsNullOrEmpty(_responseFormat))
                {
                    result = JsonConvert.DeserializeObject<ExpandoObject>(body);
                }
                else
                {
                    result = Utils.ParseFormatString(_responseFormat, body) as IDictionary<string, object>;
                }
                foreach (KeyValuePair<string, object> pair in result)
                {
                    ((IDictionary<string, object>)definition.State).Add(pair.Key, pair.Value);
                }
                succeeded = true;
            }
            catch (WebException ex)
            {
                WebResponse response = ex.Response;
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                ErrorMessage = body;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public bool IsDone()
        {
            return succeeded;
        }

        public string ErrorMessage { get; set; }
    }
}