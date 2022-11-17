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

        public Dictionary<string, string> Parameters { get; init; }
    }
}
