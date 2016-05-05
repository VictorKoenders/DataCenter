namespace DataCenter.Handlers.OAuth
{
    public class OAuthRedirectStep : IOAuthStep
    {
        private readonly string _url;

        public OAuthRedirectStep(string url)
        {
            _url = url;
        }

        public void Execute(Module module, OAuthDefinition definition)
        {
            System.Diagnostics.Process.Start(Utils.FormatString(_url, definition.State));
        }

        public bool IsDone()
        {
            return true;
        }

        public string ErrorMessage { get; }
    }
}