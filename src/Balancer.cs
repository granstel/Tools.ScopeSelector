using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace GranSteL.DialogflowBalancer
{
    public class Balancer
    {
        private readonly string[] _jsonKeysPathes;

        private readonly MemoryCache _cache;

        private static ConcurrentBag<ClientWrapper<SessionsClient>> _sessionsClients;

        public Balancer(string[] jsonKeysPathes)
        {
            _jsonKeysPathes = jsonKeysPathes;

            _cache = new MemoryCache(new MemoryCacheOptions());

            _sessionsClients = new ConcurrentBag<ClientWrapper<SessionsClient>>();

            InitSessionsClients();
        }

        private void InitSessionsClients()
        {
            foreach(var jsonKeyPath in _jsonKeysPathes)
            {
                var client = CreateSessionsClient(jsonKeyPath);

                var wrapper = new ClientWrapper<SessionsClient>(client);

                _sessionsClients.Add(wrapper);
            }
        }

        public T InvokeSessionClient<T>(string key, Func<SessionsClient, T> function)
        {
            var client = GetSessionClient(key);

            var result = function(client);

            return result;
        }

        private SessionsClient GetSessionClient(string key)
        {
            if (_cache.TryGetValue(key, out SessionsClient cachedClient))
            {
                return cachedClient;
            }
            
            var clientWrapper = _sessionsClients.OrderBy(d => d.Load).First();

            clientWrapper.Load += 1;
            
            _cache.Set(key, clientWrapper.Client, TimeSpan.FromMinutes(5));

            return clientWrapper.Client;
        }

        private SessionsClient CreateSessionsClient(string jsonKeyPath)
        {
            var credential = GoogleCredential.FromFile(jsonKeyPath).CreateScoped(SessionsClient.DefaultScopes);

            var clientBuilder = new SessionsClientBuilder
            {
                ChannelCredentials = credential.ToChannelCredentials()
            };

            var client = clientBuilder.Build();

            return client;
        }
    }
}
