using System.Collections.Generic;

namespace GranSteL.ScopesBalancer
{
    public class ScopeContext
    {
        public ScopeContext(string scopeId)
        {
            ScopeId = scopeId;
        }

        public string ScopeId { get; }

        public Dictionary<string, string> Parameters { get; set; }
    }
}
