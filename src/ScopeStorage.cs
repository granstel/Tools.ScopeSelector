using System.Collections.Concurrent;
using System.Linq;

namespace GranSteL.Tools.ScopeSelector
{
    internal static class ScopeStorage
    {
        private static ConcurrentQueue<string> ScopesIds { get; }

        static ScopeStorage()
        {
            ScopesIds = new ConcurrentQueue<string>();
        }

        public static void TryAdd(string scopeId)
        {
            if (!ScopesIds.Contains(scopeId))
            {
                ScopesIds.Enqueue(scopeId);
            }
        }

        public static string GetNextScopeId()
        {
            if (!ScopesIds.TryDequeue(out var scopeId))
            {
                return null;
            }

            ScopesIds.Enqueue(scopeId);

            return scopeId;
        }
    }
}