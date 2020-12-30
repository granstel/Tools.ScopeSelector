using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace GranSteL.DialogflowBalancer
{
    public class Balancer
    {
        private readonly string[] _jsonKeys;
        private readonly TimeSpan _expiration;
        
        private readonly MemoryCache _cache;
        private readonly ConcurrentBag<ClientWrapper<SessionsClient>> _sessionsClients;

        public Balancer(string[] jsonKeys, TimeSpan expiration)
        {
            _jsonKeys = jsonKeys;
            _expiration = expiration;

            _cache = new MemoryCache(new MemoryCacheOptions());

            _sessionsClients = new ConcurrentBag<ClientWrapper<SessionsClient>>();

            InitSessionsClients();
        }

        private void InitSessionsClients()
        {
            foreach(var jsonKeyPath in _jsonKeys)
            {
                var client = CreateSessionsClient(jsonKeyPath);

                var fileInfo = new FileInfo(jsonKeyPath);

                var wrapper = new ClientWrapper<SessionsClient>(client, fileInfo.Name);

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
            if (_cache.TryGetValue(key, out string scopeKey))
            {
                return _sessionsClients.Where(c => string.Equals(c.ScopeKey, scopeKey))
                    .Select(c => c.Client)
                    .First();
            }
            
            var clientWrapper = _sessionsClients.OrderBy(d => d.Load).First();

            clientWrapper.Load += 1;
            
            _cache.Set(key, clientWrapper.ScopeKey, _expiration);

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
