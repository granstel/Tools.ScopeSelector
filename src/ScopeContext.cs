using System.Collections.Generic;

namespace GranSteL.ScopesBalancer
{
    public class ScopeContext<T>
    {
        public ScopeContext(string scopeId)
        {
            ScopeId = scopeId;
        }

        public string ScopeId { get; }

        /// <summary>
        /// Balanced item instance
        /// </summary>
        public T ScopeItem { get; internal set; }

        public Dictionary<string, string> Parameters { get; set; }
    }
}
