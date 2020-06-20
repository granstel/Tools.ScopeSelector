using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace GranSteL.DialogflowBalancer
{
    public class Balancer
    {
        private readonly string[] _jsonKeysPathes;

        private readonly MemoryCache _cache;

        public Balancer(string[] jsonKeysPathes)
        {
            _jsonKeysPathes = jsonKeysPathes;

            _cache = new MemoryCache(new MemoryCacheOptions());

            InitClients();
        }

        private void InitClients()
        {
            throw new NotImplementedException();
        }

        private TItem GetOrCreate<TItem>(object key, Func<TItem> createItem)
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
    }
}
