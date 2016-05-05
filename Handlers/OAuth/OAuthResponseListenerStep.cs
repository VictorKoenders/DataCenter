using System.Collections.Generic;
using System.Dynamic;
using DataCenter.Web.Client;

namespace DataCenter.Handlers.OAuth
{
    public class OAuthResponseListenerStep : IOAuthStep
    {
        private readonly string _url;
        private bool loaded;

        public OAuthResponseListenerStep(string url)
        {
            _url = url;
            loaded = false;
        }

        public void Execute(Module module, OAuthDefinition definition)
        {
            
        }

        public bool IsDone()
        {
            return loaded;
        }

        public string ErrorMessage { get; }

        public void OnApiRequest(ClientRequest request, ClientResponse response, OAuthDefinition definition)
        {
            if (Utils.FormatMatches(_url, request.Url))
            {
                IDictionary<string, object> result = Utils.ParseFormatString(_url, request.Url) as IDictionary<string, object>;
                IDictionary<string, object> state = definition.State;
                foreach (KeyValuePair<string, object> pair in result)
                {
                    state.Add(pair.Key, pair.Value);
                }
                loaded = true;
            }
        }
    }
}