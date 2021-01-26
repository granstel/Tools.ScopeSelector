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
        private readonly ConcurrentQueue<Scope> _scopes;

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

            _scopes = new ConcurrentQueue<Scope>();

            _sessionsClients = new ConcurrentBag<DialogflowClientWrapper<SessionsClient>>();
            _contextsClients = new ConcurrentBag<DialogflowClientWrapper<ContextsClient>>();

            InitSessionsClient = initSessionsClient ?? DefaultInitSessionsClient;
            InitContextsClient = initContextsClient ?? DefaultInitContextsClient;

            var scopes = configuration.ClientsConfigurations.Select(c => c.ProjectId).Distinct().ToList();

            for (var i = 0; i < scopes.Count; i++)
            {
                var scopeName = scopes[i];
                var scope = new Scope(scopeName, i);
                _scopes.Enqueue(scope);
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
            var sessionsClientWrapper = GetClientWrapper(key, _sessionsClients, suggestedScopeKey);

            var result = invoke(sessionsClientWrapper.Client, sessionsClientWrapper.Context);

            return result;
        }

        public T InvokeContextsClient<T>(string key, Func<ContextsClient, DialogflowContext, T> invoke, string suggestedScopeKey = null)
        {
            var contextsClientWrapper = GetClientWrapper(key, _contextsClients, suggestedScopeKey);

            var result = invoke(contextsClientWrapper.Client, contextsClientWrapper.Context);

            return result;
        }

        private DialogflowClientWrapper<T> GetClientWrapper<T>(string key, ConcurrentBag<DialogflowClientWrapper<T>> clients, string suggestedScopeKey = null)
        {
            var cacheKey = GetCacheKey(key);

            var clientWrapper = clients.FirstOrDefault(c => string.Equals(c.ScopeKey, suggestedScopeKey));

            if (clientWrapper == null)
            {
                if (!_cache.TryGet(cacheKey, out string scopeKey))
                {
                    scopeKey = GetNextScopeKey();
                }

                clientWrapper = clients.First(c => string.Equals(c.ScopeKey, scopeKey));
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

            return scope.Name;
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
