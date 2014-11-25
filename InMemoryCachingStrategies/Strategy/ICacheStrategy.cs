using System;

namespace InMemoryCachingStrategies.Strategy
{

    public interface ICacheStrategy
    {
        /// <summary>
        /// Get an item from the cache (if cached) else reload it from data source and add it into the cache.
        /// </summary>
        /// <typeparam name="T">Type of cache item</typeparam>
        /// <param name="key">cache key</param>
        /// <param name="fetchItemFunc">Func<typeparamref name="T"/> used to reload the data from the data source (if missng from cache)</param>
        /// <param name="durationInSec">TTL value for the cache item</param>
        /// <param name="tokens">list of string to generate the final cache key</param>
        /// <returns></returns>
        T Get<T>(string key, Func<T> fetchItemFunc, int durationInSec, params string[] tokens);
    }
}
