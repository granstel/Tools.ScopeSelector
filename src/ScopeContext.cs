using System.Collections.Generic;

namespace GranSteL.Tools.ScopeSelector
{
    public class ScopeContext
    {
        public ScopeContext(string scopeId)
        {
            ScopeId = scopeId;
            Parameters = new Dictionary<string, string>();
        }

        public string ScopeId { get; }

        public Dictionary<string, string> Parameters { get; set; }
    }
}
