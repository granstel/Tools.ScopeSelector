using System.Threading.Tasks;

namespace GranSteL.ScopesBalancer
{
    public interface IScopesStorage
    {
        bool TryGetScopeKey(string invocationKey, out string scopeKey);

        Task AddAsync(string invocationKey, string scopeId);
    }
}