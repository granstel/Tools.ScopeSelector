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

        private readonly ConcurrentBag<ScopeContext<T>> _scopeItems;

        public ScopesBalancer(
            IScopesStorage storage,
            ICollection<ScopeContext<T>> scopesContexts,
            Func<ScopeContext<T>, T> initScopeItem
            )
        {
            _storage = storage;

            _scopes = new ConcurrentQueue<Scope>();

            _scopeItems = new ConcurrentBag<ScopeContext<T>>();

            var contexts = scopesContexts.DistinctBy(c => c.ScopeId).ToList();

            for (var i = 0; i < contexts.Count; i++)
            {
                var context = contexts[i];

                var scope = new Scope(context.ScopeId, i);

                _scopes.Enqueue(scope);

                var scopeItem = initScopeItem(context);

                context.ScopeItem = scopeItem;

                _scopeItems.Add(context);
            }
        }

        public TResult Invoke<TResult>(string invocationKey, Func<ScopeContext<T>, TResult> invoke, string suggestedScopeKey = null)
        {
            var context = GetScopeContext(invocationKey, suggestedScopeKey);

            var result = invoke(context);

            return result;
        }

        private ScopeContext<T> GetScopeContext(string invocationKey, string suggestedScopeKey = null)
        {
            var context = _scopeItems.FirstOrDefault(s => string.Equals(s.ScopeId, suggestedScopeKey));

            if (context == null)
            {
                if (!_storage.TryGetScopeKey(invocationKey, out string scopeKey))
                {
                    scopeKey = GetNextScopeKey();
                }

                context = _scopeItems.First(s => string.Equals(s.ScopeId, scopeKey));
            }

            _storage.Add(invocationKey, context.ScopeId);

            return context;

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
    }
}
