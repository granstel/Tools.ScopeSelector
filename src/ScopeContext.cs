using System.Collections.Generic;

namespace GranSteL.Tools.ScopeSelector
{
    public class ScopeContext
    {
        public ScopeContext(string scopeId, bool doNotAddToQueue = false)
        {
            ScopeId = scopeId;
            DoNotAddToQueue = doNotAddToQueue;
            Parameters = new Dictionary<string, string>();
        }

        public string ScopeId { get; }

        public bool DoNotAddToQueue { get; }

        private Dictionary<string, string> Parameters { get; }

        public bool TryAddParameter(string parameter, string value)
        {
            return Parameters.TryAdd(parameter, value);
        }

        public bool TryGetParameterValue(string parameter, out string value)
        {
            return Parameters.TryGetValue(parameter, out value);
        }
    }
}
