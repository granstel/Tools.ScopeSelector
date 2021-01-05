using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace GranSteL.DialogflowBalancer
{
    public class DialogflowClientsBalancer
    {
        private readonly TimeSpan _expiration;

        private readonly MemoryCache _cache;
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

            _sessionsClients = new ConcurrentBag<DialogflowClientWrapper<SessionsClient>>();
            _contextsClients = new ConcurrentBag<DialogflowClientWrapper<ContextsClient>>();

            InitSessionsClient = initSessionsClient ?? DefaultInitSessionsClient;
            InitContextsClient = initContextsClient ?? DefaultInitContextsClient;

            foreach (var clientsConfiguration in configuration.ClientsConfigurations)
            {
                var context = new DialogflowContext(clientsConfiguration);

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
            DialogflowClientWrapper<SessionsClient> clientWrapper;

            if (_cache.TryGetValue(key, out string scopeKey))
            {
                clientWrapper = _sessionsClients.First(c => string.Equals(c.ScopeKey, scopeKey));
            }
            else
            {
                clientWrapper = _sessionsClients.OrderBy(d => d.Load).First();
            }

            clientWrapper.Load += 1;

            _cache.Set(key, clientWrapper.ScopeKey, _expiration);

            return clientWrapper;
        }

        private DialogflowClientWrapper<ContextsClient> GetContextsClientWrapper(string key)
        {
            DialogflowClientWrapper<ContextsClient> clientWrapper;

            if (_cache.TryGetValue(key, out string scopeKey))
            {
                clientWrapper = _contextsClients.First(c => string.Equals(c.ScopeKey, scopeKey));
            }
            else
            {
                clientWrapper = _contextsClients.OrderBy(d => d.Load).First();
            }

            clientWrapper.Load += 1;

            _cache.Set(key, clientWrapper.ScopeKey, _expiration);

            return clientWrapper;
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
