using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GranSteL.DialogflowBalancer
{
    public class DialogflowClientsBalancer
    {
        private readonly TimeSpan _expiration;

        private readonly MemoryCache _cache;
        private readonly ConcurrentDictionary<string, int> _scopeLoads;

        private readonly ConcurrentBag<DialogflowClientWrapper<SessionsClient>> _sessionsClients;
        private readonly ConcurrentBag<DialogflowClientWrapper<ContextsClient>> _contextsClients;

        public Func<DialogflowContext, SessionsClient> InitSessionsClient;
        public Func<DialogflowContext, ContextsClient> InitContextsClient;

        public DialogflowClientsBalancer(
            DialogflowBalancerConfiguration configuration,
            Func<DialogflowContext, SessionsClient> initSessionsClient = null,
            Func<DialogflowContext, ContextsClient> initContextsClient = null
            )
        {
            _expiration = configuration.ScopeExpiration;

            _cache = new MemoryCache(new MemoryCacheOptions());

            _scopeLoads = new ConcurrentDictionary<string, int>();

            _sessionsClients = new ConcurrentBag<DialogflowClientWrapper<SessionsClient>>();
            _contextsClients = new ConcurrentBag<DialogflowClientWrapper<ContextsClient>>();

            InitSessionsClient = initSessionsClient ?? DefaultInitSessionsClient;
            InitContextsClient = initContextsClient ?? DefaultInitContextsClient;

            foreach (var clientsConfiguration in configuration.ClientsConfigurations)
            {
                var context = new DialogflowContext(clientsConfiguration);
                
                _scopeLoads.GetOrAdd(context.ProjectId, 0);

                InitSessionsClientInternal(context);
                InitContextsClientInternal(context);
            }
        }

        public T InvokeSessionsClient<T>(string key, Func<SessionsClient, DialogflowContext, T> invoke)
        {
            var sessionsClientWrapper = GetSessionsClientWrapper(key);

            var result = invoke(sessionsClientWrapper.Client, sessionsClientWrapper.Context);

            return result;
        }

        public T InvokeContextsClient<T>(string key, Func<ContextsClient, DialogflowContext, T> invoke)
        {
            var contextsClientWrapper = GetContextsClientWrapper(key);

            var result = invoke(contextsClientWrapper.Client, contextsClientWrapper.Context);

            return result;
        }

        private DialogflowClientWrapper<SessionsClient> GetSessionsClientWrapper(string key)
        {
            if (!_cache.TryGetValue(key, out string scopeKey))
            {
                scopeKey = _scopeLoads.OrderBy(s => s.Value).Select(s => s.Key).First();

                _scopeLoads[scopeKey] += 1;
            }

            var clientWrapper = _sessionsClients.First(c => string.Equals(c.ScopeKey, scopeKey));

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _expiration
            };

            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = EvictionCallback,
                State = $"{nameof(clientWrapper.ScopeKey)}"
            });

            _cache.Set(key, clientWrapper.ScopeKey, options);

            return clientWrapper;
        }

        private DialogflowClientWrapper<ContextsClient> GetContextsClientWrapper(string key)
        {
            if (!_cache.TryGetValue(key, out string scopeKey))
            {
                scopeKey = _scopeLoads.OrderBy(s => s.Value).Select(s => s.Key).First();

                _scopeLoads[scopeKey] += 1;
            }

            var clientWrapper = _contextsClients.First(c => string.Equals(c.ScopeKey, scopeKey));

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _expiration
            };

            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = EvictionCallback,
                State = $"{nameof(clientWrapper.ScopeKey)}"
            });

            _cache.Set(key, clientWrapper.ScopeKey, options);

            return clientWrapper;
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (reason == EvictionReason.Expired && string.Equals(state, $"{nameof(DialogflowClientWrapper<object>.ScopeKey)}") && value is string scopeKey)
            {
                _scopeLoads[scopeKey] -= 1;
            }
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
    }
}
