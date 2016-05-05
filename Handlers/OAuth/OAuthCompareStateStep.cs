using System.Collections.Generic;

namespace DataCenter.Handlers.OAuth
{
    public class OAuthCompareStateStep : IOAuthStep
    {
        private readonly string _stateVar1;
        private readonly string _stateVar2;

        private bool _succeeded;

        public OAuthCompareStateStep(string stateVar1, string stateVar2)
        {
            _stateVar1 = stateVar1;
            _stateVar2 = stateVar2;
        }

        public void Execute(Module module, OAuthDefinition definition)
        {
            IDictionary<string, object> state = definition.State;
            _succeeded = Equals(state[_stateVar1], state[_stateVar2]);

            if (!_succeeded)
            {
                ErrorMessage = string.Format("\"{0}\" does not match \"{1}\"", state[_stateVar1], state[_stateVar2]);
            }
        }

        public bool IsDone()
        {
            return _succeeded;
        }

        public string ErrorMessage { get; set; }
    }
}