namespace GranSteL.Tools.ScopeSelector
{
    public interface IScopesStorage
    {
        bool TryGetScopeKey(string invocationKey, out string scopeKey);

        void Add(string invocationKey, string scopeId);
    }
}