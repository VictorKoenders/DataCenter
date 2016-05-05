using System.Collections.Generic;
using System.Dynamic;
using DataCenter.Web.Client;

namespace DataCenter.Handlers.OAuth
{
    public class  OAuthDefinition
    {
        private readonly Module _module;
        public ExpandoObject State { get; set; }
        public List<IOAuthStep> Steps { get; set; }
        public string SucceededEventName { get; set; }
        public string FailedEventName { get; set; }
        public int StepIndex { get; set; }
        
        public bool IsAuthenticating { get; set; }
        public bool Failed { get; set; }

        public OAuthDefinition(Module module, ExpandoObject data)
        {
            _module = module;
            
            IDictionary<string, object> a = data;
            State = a.ContainsKey("state") ? a["state"] as ExpandoObject ?? new ExpandoObject() : new ExpandoObject();

            if (a.ContainsKey("steps"))
            {
                object[] steps = a["steps"] as object[];
                if (steps == null) return;
                Steps = new List<IOAuthStep>(steps.Length);
                foreach (object step in steps)
                {
                    Steps.Add(step as IOAuthStep);
                }
            }

            if (a.ContainsKey("eventNames"))
            {
                IDictionary<string, object> names = a["eventNames"] as IDictionary<string, object>;
                FailedEventName = names["fail"] as string;
                SucceededEventName = names["success"] as string;
            }
        }

        public void connect()
        {
            if (Steps != null && Steps.Count > 0)
            {
                IsAuthenticating = true;
                StepIndex = 0;
                Execute();
            }
        }

        public IOAuthStep CurrentStep
        {
            get
            {
                if (!IsAuthenticating) return null;
                return Steps[StepIndex];
            }
        }

        public void OnApiRequest(ClientRequest request, ClientResponse response)
        {
            if (CurrentStep is OAuthResponseListenerStep)
            {
                ((OAuthResponseListenerStep) CurrentStep).OnApiRequest(request, response, this);
                Execute();
                response.SetHeader("Content-Type", "text/html");
                response.Body = "<html><body><script>window.close();</script></body></html>";
                response.Flush();
            }
        }

        private void Execute()
        {
            if (!IsAuthenticating) return;

            IOAuthStep step = Steps[StepIndex];
            step.Execute(_module, this);

            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                EmitFail(step.ErrorMessage);
                return;
            }

            if (!step.IsDone()) return;

            StepIndex++;
            if (StepIndex >= Steps.Count)
            {
                EmitSuccess();
                return;
            }

            Execute();
        }

        private void EmitFail(string errorMessage)
        {
            IsAuthenticating = false;
            Failed = true;
            _module.Emit(FailedEventName, new { }, new
            {
                step = Steps[StepIndex].GetType().Name,
                reason = errorMessage
            });
        }

        private void EmitSuccess()
        {
            IsAuthenticating = false;
            _module.Emit(SucceededEventName, new { }, new
            {
                state = State
            });
        }
    }
}