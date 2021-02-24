using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GranSteL.Tools.ScopeSelector.Extensions;

namespace GranSteL.Tools.ScopeSelector
{
    public class ScopesSelector<T>
    {
        private static readonly ConcurrentQueue<string> ScopesIds;

        private readonly IScopesStorage _storage;

        private readonly ConcurrentBag<ScopeItemWrapper<T>> _scopeItems;

        static ScopesSelector()
        {
            ScopesIds = new ConcurrentQueue<string>();
        }

        public ScopesSelector(
            IScopesStorage storage,
            ICollection<ScopeContext> scopesContexts,
            Func<ScopeContext, T> initScopeItem
            )
        {
            _storage = storage;

            _scopeItems = new ConcurrentBag<ScopeItemWrapper<T>>();

            var contexts = scopesContexts.DistinctBy(c => c.ScopeId).ToList();

            foreach (var context in contexts)
            {
                if (!ScopesIds.Contains(context.ScopeId))
                {
                    ScopesIds.Enqueue(context.ScopeId);
                }

                var scopeItem = initScopeItem(context);

                var wrapper = new ScopeItemWrapper<T>(scopeItem, context);

                _scopeItems.Add(wrapper);
            }
        }

        public TResult Invoke<TResult>(string invocationKey, Func<T, ScopeContext, TResult> invoke, string suggestedScopeKey = null)
        {
            var scopeWrapper = GetScopeItem(invocationKey, suggestedScopeKey);

            var result = invoke(scopeWrapper.ScopeItem, scopeWrapper.Context);

            return result;
        }

        private ScopeItemWrapper<T> GetScopeItem(string invocationKey, string suggestedScopeKey = null)
        {
            var scopeItem = _scopeItems.FirstOrDefault(s => string.Equals(s.Context.ScopeId, suggestedScopeKey));

            if (scopeItem == null)
            {
                if (!_storage.TryGetScopeKey(invocationKey, out string scopeKey))
                {
                    scopeKey = SelectScope();
                }

                scopeItem = _scopeItems.First(s => string.Equals(s.Context.ScopeId, scopeKey));
            }

            _storage.Add(invocationKey, scopeItem.Context.ScopeId);

            return scopeItem;

        }

        private string SelectScope()
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
