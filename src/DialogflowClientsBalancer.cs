﻿using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using System;
using System.Collections.Concurrent;
using System.Linq;
using GranSteL.Helpers.Redis;
using GranSteL.DialogflowBalancer.Extensions;

namespace GranSteL.DialogflowBalancer
{
    public class DialogflowClientsBalancer
    {
        private readonly TimeSpan _expiration;

        private readonly IRedisCacheService _cache;
        private readonly ConcurrentBag<Scope> _scopes;

        private readonly ConcurrentBag<DialogflowClientWrapper<SessionsClient>> _sessionsClients;
        private readonly ConcurrentBag<DialogflowClientWrapper<ContextsClient>> _contextsClients;

        public Func<DialogflowContext, SessionsClient> InitSessionsClient;
        public Func<DialogflowContext, ContextsClient> InitContextsClient;

        public DialogflowClientsBalancer(
            IRedisCacheService cache,
            DialogflowBalancerConfiguration configuration,
            Func<DialogflowContext, SessionsClient> initSessionsClient = null,
            Func<DialogflowContext, ContextsClient> initContextsClient = null
            )
        {
            _expiration = configuration.ScopeExpiration;

            _cache = cache;

            _scopes = new ConcurrentBag<Scope>();

            _sessionsClients = new ConcurrentBag<DialogflowClientWrapper<SessionsClient>>();
            _contextsClients = new ConcurrentBag<DialogflowClientWrapper<ContextsClient>>();

            InitSessionsClient = initSessionsClient ?? DefaultInitSessionsClient;
            InitContextsClient = initContextsClient ?? DefaultInitContextsClient;

            var scopes = configuration.ClientsConfigurations.Select(c => c.ProjectId).Distinct().ToList();

            for (var i = 0; i < scopes.Count; i++)
            {
                var scopeName = scopes[i];
                var scope = new Scope(scopeName, i);
                _scopes.Add(scope);
            }

            foreach (var clientsConfiguration in configuration.ClientsConfigurations)
            {
                var context = new DialogflowContext(clientsConfiguration);

                InitSessionsClientInternal(context);
                InitContextsClientInternal(context);
            }
        }

        public T InvokeSessionsClient<T>(string key, Func<SessionsClient, DialogflowContext, T> invoke, string suggestedScopeKey = null)
        {
            var sessionsClientWrapper = GetSessionsClientWrapper(key, suggestedScopeKey);

            var result = invoke(sessionsClientWrapper.Client, sessionsClientWrapper.Context);

            return result;
        }

        public T InvokeContextsClient<T>(string key, Func<ContextsClient, DialogflowContext, T> invoke, string suggestedScopeKey = null)
        {
            var contextsClientWrapper = GetContextsClientWrapper(key, suggestedScopeKey);

            var result = invoke(contextsClientWrapper.Client, contextsClientWrapper.Context);

            return result;
        }

        public string GetScopeKey(string key)
        {
            var cacheKey = GetCacheKey(key);

            if (!_cache.TryGet(cacheKey, out string scopeKey))
            {
                scopeKey = GetNextScopeKey();
            }

            return scopeKey;
        }

        private DialogflowClientWrapper<SessionsClient> GetSessionsClientWrapper(string key, string suggestedScopeKey = null)
        {
            DialogflowClientWrapper<SessionsClient> clientWrapper;

            string scopeKey;

            var cacheKey = GetCacheKey(key);

            if (!string.IsNullOrEmpty(suggestedScopeKey))
            {
                scopeKey = suggestedScopeKey;

                clientWrapper = GetClientWrapper(scopeKey, cacheKey, _sessionsClients);

                if (clientWrapper != null)
                {
                    return clientWrapper;
                }
            }

            if (!_cache.TryGet(cacheKey, out scopeKey))
            {
                scopeKey = GetNextScopeKey();
            }

            clientWrapper = GetClientWrapper(scopeKey, cacheKey, _sessionsClients);

            return clientWrapper;
        }

        private DialogflowClientWrapper<ContextsClient> GetContextsClientWrapper(string key, string suggestedScopeKey = null)
        {
            DialogflowClientWrapper<ContextsClient> clientWrapper;

            string scopeKey;

            var cacheKey = GetCacheKey(key);

            if (!string.IsNullOrEmpty(suggestedScopeKey))
            {
                scopeKey = suggestedScopeKey;

                clientWrapper = GetClientWrapper(scopeKey, cacheKey, _contextsClients);

                if (clientWrapper != null)
                {
                    return clientWrapper;
                }
            }

            if (!_cache.TryGet(cacheKey, out scopeKey))
            {
                scopeKey = GetNextScopeKey();
            }

            clientWrapper = GetClientWrapper(scopeKey, cacheKey, _contextsClients);

            return clientWrapper;
        }

        private DialogflowClientWrapper<T> GetClientWrapper<T>(string scopeKey, string cacheKey, ConcurrentBag<DialogflowClientWrapper<T>> contextsClients)
        {
            var clientWrapper = contextsClients.First(c => string.Equals(c.ScopeKey, scopeKey));

            if (clientWrapper == null)
            {
                return null;
            }

            _cache.AddAsync(cacheKey, clientWrapper.ScopeKey, _expiration).Forget();

            return clientWrapper;

        }

        private string GetNextScopeKey()
        {
            var orderedScopes = _scopes.OrderBy(s => s.Priority).ToList();

            var scopeKey = string.Empty;

            for (var i = 0; i < orderedScopes.Count; i++)
            {
                var scope = orderedScopes[i];

                if (i == 0)
                {
                    scopeKey = scope.Name;
                    scope.Priority = orderedScopes.Count - 1;
                    continue;
                }

                scope.Priority -= 1;
            }

            return scopeKey;
        }

        private SessionsClient DefaultInitSessionsClient(DialogflowContext context)
        {
            var credential = GoogleCredential.FromFile(context.JsonPath).CreateScoped(SessionsClient.DefaultScopes);

            var clientBuilder = new SessionsClientBuilder
            {
                ChannelCredentials = credential.ToChannelCredentials()
            };

            var client = clientBuilder.Build();

            return client;
        }

        private ContextsClient DefaultInitContextsClient(DialogflowContext context)
        {
            var credential = GoogleCredential.FromFile(context.JsonPath).CreateScoped(ContextsClient.DefaultScopes);

            var clientBuilder = new ContextsClientBuilder
            {
                ChannelCredentials = credential.ToChannelCredentials()
            };

            var client = clientBuilder.Build();

            return client;
        }

        private void InitSessionsClientInternal(DialogflowContext context)
        {
            var client = InitSessionsClient(context);

            var wrapper = new DialogflowClientWrapper<SessionsClient>(client, context);

            _sessionsClients.Add(wrapper);
        }

        private void InitContextsClientInternal(DialogflowContext context)
        {
            var client = InitContextsClient(context);

            var wrapper = new DialogflowClientWrapper<ContextsClient>(client, context);

            _contextsClients.Add(wrapper);
        }

        private string GetCacheKey(string key)
        {
            return $"scopes:{key}";
        }
    }
}
