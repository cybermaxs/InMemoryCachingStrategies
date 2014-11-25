using System;

namespace InMemoryCachingStrategies.Strategy
{
    internal class BasicCacheStrategy : BaseStrategy, ICacheStrategy
    {
        public BasicCacheStrategy(InMemoryCache cache=null) : base(cache ?? new InMemoryCache())
        {

        }

        public T Get<T>(string key, Func<T> fetchItemFunc, int durationInSec, params string[] tokens)
        {
            var cacheKey = this.CreateKey(key, tokens);
            var item = this.Cache.Get<T>(key);
            if (this.IsDefault(item))
            {
                item = fetchItemFunc();
                this.Cache.Set(key, item, durationInSec, false);
            }
            return item;
        }
    }
}
