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

        private readonly ConcurrentBag<ScopeItemWrapper<T>> _scopeItems;

        private readonly Func<ScopeContext, T> _initScopeItem;

        public ScopesBalancer(
            IRedisCacheService cache,
            ICollection<ScopeContext> scopesContexts,
            Func<ScopeContext, T> initScopeItem
            )
        {
            _expiration = TimeSpan.MaxValue;

            _cache = cache;

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

        public TResult InvokeScopeItem<TResult>(string key, Func<T, ScopeContext, TResult> invoke, string suggestedScopeKey = null)
        {
            var scopeWrapper = GetScopeItem(key, suggestedScopeKey);

            var result = invoke(scopeWrapper.ScopeItem, scopeWrapper.Context);

            return result;
        }

        private ScopeItemWrapper<T> GetScopeItem(string key, string suggestedScopeKey = null)
        {
            var cacheKey = GetCacheKey(key);

            var scopeItem = _scopeItems.FirstOrDefault(s => string.Equals(s.Context.ScopeId, suggestedScopeKey));

            if (scopeItem == null)
            {
                if (!_cache.TryGet(cacheKey, out string scopeKey))
                {
                    scopeKey = GetNextScopeKey();
                }

                scopeItem = _scopeItems.First(s => string.Equals(s.Context.ScopeId, scopeKey));
            }

            _cache.AddAsync(cacheKey, scopeItem.Context.ScopeId, _expiration).Forget();

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

        private string GetCacheKey(string key)
        {
            return $"scopes:{key}";
        }
    }
}
