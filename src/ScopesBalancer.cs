using System;
using System.Collections.Concurrent;
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

        public Func<ScopeBalancerContext, T> InitBalancedScopes;

        public ScopesBalancer(
            IRedisCacheService cache,
            ScopesBalancerConfiguration balancerConfiguration,
            Func<ScopeBalancerContext, T> initBalancedScope
            )
        {
            _expiration = balancerConfiguration.ScopeExpiration;

            _cache = cache;

            _scopes = new ConcurrentQueue<Scope>();

            _balancedScopes = new ConcurrentBag<ScopeWrapper<T>>();

            InitBalancedScopes = initBalancedScope;

            var configurations = balancerConfiguration.ClientsConfigurations.DistinctBy(c => c.ScopeId).ToList();

            for (var i = 0; i < configurations.Count; i++)
            {
                var configuration = configurations[i];

                var scope = new Scope(configuration.ScopeId, i);

                _scopes.Enqueue(scope);

                var context = new ScopeBalancerContext(configuration);

                InitSessionsClientInternal(context);
            }
        }

        public TResult InvokeScopeItem<TResult>(string key, Func<T, ScopeBalancerContext, TResult> invoke, string suggestedScopeKey = null)
        {
            var scopeWrapper = GetScopeWrapper(key, suggestedScopeKey);

            var result = invoke(scopeWrapper.BalancedScopeItem, scopeWrapper.Context);

            return result;
        }

        private ScopeWrapper<T> GetScopeWrapper(string key, string suggestedScopeKey = null)
        {
            var cacheKey = GetCacheKey(key);

            var clientWrapper = _balancedScopes.FirstOrDefault(c => string.Equals(c.ScopeKey, suggestedScopeKey));

            if (clientWrapper == null)
            {
                if (!_cache.TryGet(cacheKey, out string scopeKey))
                {
                    scopeKey = GetNextScopeKey();
                }

                clientWrapper = _balancedScopes.First(c => string.Equals(c.ScopeKey, scopeKey));
            }

            _cache.AddAsync(cacheKey, clientWrapper.ScopeKey, _expiration).Forget();

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

        private void InitSessionsClientInternal(ScopeBalancerContext context)
        {
            var initBalancedScopes = InitBalancedScopes(context);

            var wrapper = new ScopeWrapper<T>(initBalancedScopes, context);

            _balancedScopes.Add(wrapper);
        }

        private string GetCacheKey(string key)
        {
            return $"scopes:{key}";
        }
    }
}
