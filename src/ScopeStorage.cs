using System.Collections.Concurrent;

namespace GranSteL.Tools.ScopeSelector
{
    internal static class ScopeStorage
    {
        internal static ConcurrentQueue<string> ScopesIds { get; }

        static ScopeStorage()
        {
            ScopesIds = new ConcurrentQueue<string>();
        }
    }
}