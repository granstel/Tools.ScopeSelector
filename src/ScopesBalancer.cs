using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GranSteL.ScopesBalancer.Extensions;

namespace GranSteL.ScopesBalancer
{
    public class ScopesBalancer<T>
    {
        private static readonly ConcurrentQueue<string> ScopesIds;

        private readonly IScopesStorage _storage;

        private readonly ConcurrentBag<ScopeItemWrapper<T>> _scopeItems;

        private readonly Func<ScopeContext, T> _initScopeItem;

        static ScopesBalancer()
        {
            ScopesIds = new ConcurrentQueue<string>();
        }

        public ScopesBalancer(
            IScopesStorage storage,
            ICollection<ScopeContext> scopesContexts,
            Func<ScopeContext, T> initScopeItem
            )
        {
            _storage = storage;

            _scopeItems = new ConcurrentBag<ScopeItemWrapper<T>>();

            _initScopeItem = initScopeItem;

            var contexts = scopesContexts.DistinctBy(c => c.ScopeId).ToList();

            foreach (var context in contexts)
            {
                if (!ScopesIds.Contains(context.ScopeId))
                {
                    ScopesIds.Enqueue(context.ScopeId);
                }

                InitScopeItemInternal(context);
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
                    scopeKey = GetNextScopeKey();
                }

                scopeItem = _scopeItems.First(s => string.Equals(s.Context.ScopeId, scopeKey));
            }

            _storage.Add(invocationKey, scopeItem.Context.ScopeId);

            return scopeItem;

        }

        private string GetNextScopeKey()
        {
            if (!ScopesIds.TryDequeue(out var scopeId))
            {
                return null;
            }

            ScopesIds.Enqueue(scopeId);

            return scopeId;
        }

        private void InitScopeItemInternal(ScopeContext context)
        {
            var scopeItem = _initScopeItem(context);

            var wrapper = new ScopeItemWrapper<T>(scopeItem, context);

            _scopeItems.Add(wrapper);
        }
    }
}
