namespace DataCenter.Handlers.OAuth
{
    public interface IOAuthStep
    {
        void Execute(Module module, OAuthDefinition definition);
        bool IsDone();
        string ErrorMessage { get; }
    }
}