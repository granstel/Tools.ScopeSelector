using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GranSteL.Helpers.Redis;
using GranSteL.ScopesBalancer.Extensions;

namespace GranSteL.ScopesBalancer
{
    public class ScopesBalancer<T>
    {
        private readonly TimeSpan _expiration;

        private readonly IRedisCacheService _cache;
        private readonly ConcurrentQueue<Scope> _scopes;

        private readonly ConcurrentBag<ScopeWrapper<T>> _balancedScopes;

        private readonly Func<ScopeContext, T> _initBalancedScopes;

        public ScopesBalancer(
            IRedisCacheService cache,
            ICollection<ScopeContext> scopesContexts,
            Func<ScopeContext, T> initBalancedScope
            )
        {
            _expiration = TimeSpan.MaxValue;

            _cache = cache;

            _scopes = new ConcurrentQueue<Scope>();

            _balancedScopes = new ConcurrentBag<ScopeWrapper<T>>();

            _initBalancedScopes = initBalancedScope;

            var contexts = scopesContexts.DistinctBy(c => c.ScopeId).ToList();

            for (var i = 0; i < contexts.Count; i++)
            {
                var context = contexts[i];

                var scope = new Scope(context.ScopeId, i);

                _scopes.Enqueue(scope);

                InitSessionsClientInternal(context);
            }
        }

        public TResult InvokeScopeItem<TResult>(string key, Func<T, ScopeContext, TResult> invoke, string suggestedScopeKey = null)
        {
            var scopeWrapper = GetScopeWrapper(key, suggestedScopeKey);

            var result = invoke(scopeWrapper.BalancedScopeItem, scopeWrapper.Context);

            return result;
        }

        private ScopeWrapper<T> GetScopeWrapper(string key, string suggestedScopeKey = null)
        {
            var cacheKey = GetCacheKey(key);

            var clientWrapper = _balancedScopes.FirstOrDefault(c => string.Equals(c.ScopeId, suggestedScopeKey));

            if (clientWrapper == null)
            {
                if (!_cache.TryGet(cacheKey, out string scopeKey))
                {
                    scopeKey = GetNextScopeKey();
                }

                clientWrapper = _balancedScopes.First(c => string.Equals(c.ScopeId, scopeKey));
            }

            _cache.AddAsync(cacheKey, clientWrapper.ScopeId, _expiration).Forget();

            return clientWrapper;

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

        private void InitSessionsClientInternal(ScopeContext context)
        {
            var initBalancedScopes = _initBalancedScopes(context);

            var wrapper = new ScopeWrapper<T>(initBalancedScopes, context);

            _balancedScopes.Add(wrapper);
        }

        private string GetCacheKey(string key)
        {
            return $"scopes:{key}";
        }
    }
}
