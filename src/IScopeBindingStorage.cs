using System.Collections.Generic;

namespace GranSteL.Tools.ScopeSelector
{
    public interface IScopeBindingStorage
    {
        bool TryGet(string bindingKey, out ICollection<string> scopeId);

        void Add(string bindingKey, params string[] scopeId);
    }
}