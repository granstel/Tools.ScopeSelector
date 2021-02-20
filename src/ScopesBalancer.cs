using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GranSteL.ScopesBalancer.Extensions;

namespace GranSteL.ScopesBalancer
{
    public class ScopesBalancer<T>
    {
        private readonly IScopesStorage _storage;
        private readonly ConcurrentQueue<Scope> _scopes;

        private readonly ConcurrentBag<ScopeItemWrapper<T>> _scopeItems;

        private readonly Func<ScopeContext, T> _initScopeItem;

        public ScopesBalancer(
            IScopesStorage storage,
            ICollection<ScopeContext> scopesContexts,
            Func<ScopeContext, T> initScopeItem
            )
        {
            _storage = storage;

            _scopes = new ConcurrentQueue<Scope>();

            _scopeItems = new ConcurrentBag<ScopeItemWrapper<T>>();

            _initScopeItem = initScopeItem;

            var contexts = scopesContexts.DistinctBy(c => c.ScopeId).ToList();

            for (var i = 0; i < contexts.Count; i++)
            {
                var context = contexts[i];

                var scope = new Scope(context.ScopeId, i);

                _scopes.Enqueue(scope);

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
            if (!_scopes.TryDequeue(out var scope))
            {
                return null;
            }

            _scopes.Enqueue(scope);

            return scope.Id;
        }

        private void InitScopeItemInternal(ScopeContext context)
        {
            var scopeItem = _initScopeItem(context);

            var wrapper = new ScopeItemWrapper<T>(scopeItem, context);

            _scopeItems.Add(wrapper);
        }
    }
}
