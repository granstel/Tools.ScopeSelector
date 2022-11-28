using System.Collections.Generic;

namespace GranSteL.Tools.ScopeSelector
{
    /// <summary>
    /// Context of a scope
    /// </summary>
    public class ScopeContext
    {
        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="scopeId">Unique identifier of a scope</param>
        /// <param name="doNotAddToQueue">Whether to add an instance to the queue</param>
        public ScopeContext(string scopeId, bool doNotAddToQueue = false)
        {
            ScopeId = scopeId;
            DoNotAddToQueue = doNotAddToQueue;
            Parameters = new Dictionary<string, string>();
        }

        /// <summary>
        /// Unique identifier of a scope
        /// </summary>
        public string ScopeId { get; }

        /// <summary>
        /// Whether to add an instance to the queue
        /// </summary>
        public bool DoNotAddToQueue { get; }

        /// <summary>
        /// Any parameters at the context
        /// </summary>
        private Dictionary<string, string> Parameters { get; }

        /// <summary>
        /// Attempts to add the specified parameter and it's value
        /// </summary>
        /// <param name="name">The name of the parameter to add</param>
        /// <param name="value">The value of the parameter to add</param>
        /// <returns><see langword="true"/> if the name/value pair was added to the <see cref="Parameters"/> successfully;
        /// otherwise, <see langword="false"/></returns>
        public bool TryAddParameter(string name, string value)
        {
            return Parameters.TryAdd(name, value);
        }

        /// <summary>
        /// Gets the value associated with the specified name
        /// </summary>
        /// <param name="name">The name of the value to get</param>
        /// <param name="value">The value of the parameter to add</param>
        /// <returns><see langword="true"/> if the <see cref="Parameters"/> contains an element with the specified name;
        /// otherwise, <see langword="false"/></returns>
        public bool TryGetParameterValue(string name, out string value)
        {
            return Parameters.TryGetValue(name, out value);
        }
    }
}
