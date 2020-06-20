using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Grpc.Auth;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GranSteL.DialogflowBalancer
{
    public class Balancer
    {
        private readonly string[] _jsonKeysPathes;

        private readonly MemoryCache _cache;

        private static IDictionary<List<string>, SessionsClient> clientsDictinoary;

        public Balancer(string[] jsonKeysPathes)
        {
            _jsonKeysPathes = jsonKeysPathes;

            _cache = new MemoryCache(new MemoryCacheOptions());

            clientsDictinoary = new Dictionary<List<string>, SessionsClient>();

            InitClients();
        }

        private void InitClients()
        {
            foreach(var jsonKeyPath in _jsonKeysPathes)
            {
                var client = CreateClient(jsonKeyPath);

                clientsDictinoary.Add(new List<string>(), client);

                //var info = new FileInfo(jsonKeyPath);
                //var key = info.Name;

                //var client = GetOrCreate(key, () => CreateClient(jsonKeyPath));
            }
        }

        public async Task<DetectIntentResponse> DetectIntentAsync(DetectIntentRequest request)
        {
            var client = GetClient(request.Session);

            var response = await client.DetectIntentAsync(request);

            return response;
        }

        public DetectIntentResponse DetectIntent(DetectIntentRequest request)
        {
            var client = GetClient(request.Session);

            var response = client.DetectIntent(request);

            return response;
        }

        private SessionsClient GetClient(string session)
        {
            var maxLoaded = clientsDictinoary.Max(d => d.Key.Count);

            var client = clientsDictinoary.Where(d => d.Key.Contains(session)).Select(d => d.Value).FirstOrDefault() ??
                         clientsDictinoary.Where(d => d.Key.Count < maxLoaded).Select(d => d.Value).FirstOrDefault() ??
                         clientsDictinoary.Select(d => d.Value).FirstOrDefault();

            return client;
        }

        private TItem GetOrCreate<TItem>(string key, Func<TItem> createItem)
        {
            TItem cacheEntry;
            if (!_cache.TryGetValue(key, out cacheEntry)) // Ищем ключ в кэше.
            {
                // Ключ отсутствует в кэше, поэтому получаем данные.
                cacheEntry = createItem();

                // Сохраняем данные в кэше. 
                _cache.Set(key, cacheEntry);
            }
            return cacheEntry;
        }

        private SessionsClient CreateClient(string jsonKeyPath)
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
