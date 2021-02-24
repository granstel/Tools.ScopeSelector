namespace GranSteL.Tools.ScopeSelector
{
    public interface IScopeBindingStorage
    {
        bool TryGet(string bindingKey, out string scopeId);

        void Add(string bindingKey, string scopeId);
    }
}