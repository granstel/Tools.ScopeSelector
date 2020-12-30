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
        private readonly TimeSpan _expiration;
        
        private readonly MemoryCache _cache;
        private readonly ConcurrentBag<ClientWrapper<SessionsClient>> _sessionsClients;
        private readonly ConcurrentBag<ClientWrapper<ContextsClient>> _contextsClients;

        public Balancer(string[] jsonKeys, TimeSpan expiration)
        {
            _expiration = expiration;

            _cache = new MemoryCache(new MemoryCacheOptions());

            _sessionsClients = new ConcurrentBag<ClientWrapper<SessionsClient>>();
            _contextsClients = new ConcurrentBag<ClientWrapper<ContextsClient>>();

            foreach(var jsonKeyPath in jsonKeys)
            {
                InitSessionsClient(jsonKeyPath);
                InitContextsClient(jsonKeyPath);
            }
        }

        public T InvokeSessionsClient<T>(string key, Func<SessionsClient, T> invoke)
        {
            var client = GetSessionsClient(key);

            var result = invoke(client);

            return result;
        }
        public T InvokeContextsClient<T>(string key, Func<ContextsClient, T> invoke)
        {
            var client = GetContextsClient(key);

            var result = invoke(client);

            return result;
        }

        private SessionsClient GetSessionsClient(string key)
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
        
        private ContextsClient GetContextsClient(string key)
        {
            if (_cache.TryGetValue(key, out string scopeKey))
            {
                return _contextsClients.Where(c => string.Equals(c.ScopeKey, scopeKey))
                    .Select(c => c.Client)
                    .First();
            }
            
            var clientWrapper = _contextsClients.OrderBy(d => d.Load).First();

            clientWrapper.Load += 1;
            
            _cache.Set(key, clientWrapper.ScopeKey, _expiration);

            return clientWrapper.Client;
        }

        private void InitSessionsClient(string jsonKeyPath)
        {
            var credential = GoogleCredential.FromFile(jsonKeyPath).CreateScoped(SessionsClient.DefaultScopes);

            var clientBuilder = new SessionsClientBuilder
            {
                ChannelCredentials = credential.ToChannelCredentials()
            };

            var client = clientBuilder.Build();

            var fileInfo = new FileInfo(jsonKeyPath);
            var wrapper = new ClientWrapper<SessionsClient>(client, fileInfo.Name);

            _sessionsClients.Add(wrapper);
        }

        private void InitContextsClient(string jsonKeyPath)
        {
            var credential = GoogleCredential.FromFile(jsonKeyPath).CreateScoped(ContextsClient.DefaultScopes);

            var clientBuilder = new ContextsClientBuilder
            {
                ChannelCredentials = credential.ToChannelCredentials()
            };

            var client = clientBuilder.Build();

            var fileInfo = new FileInfo(jsonKeyPath);
            var wrapper = new ClientWrapper<ContextsClient>(client, fileInfo.Name);

            _contextsClients.Add(wrapper);
        }
    }
}
