namespace GranSteL.ScopesBalancer
{
    public interface IScopesStorage
    {
        bool TryGetScopeKey(string invocationKey, out string scopeKey);

        void Add(string invocationKey, string scopeId);
    }
}