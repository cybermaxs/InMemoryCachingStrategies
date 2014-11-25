using System;

namespace InMemoryCachingStrategies.Strategy
{
    internal class DoubleCheckedLockingCacheStrategy : BaseStrategy, ICacheStrategy
    {
        public int? SyncLockDuration { get; set; }

        public DoubleCheckedLockingCacheStrategy(InMemoryCache cache = null, int? syncLockDuration = null)
            : base(cache ?? new InMemoryCache())
        {
            this.SyncLockDuration = syncLockDuration;
        }

        public T Get<T>(string key, Func<T> fetchItemFunc, int durationInSec, params string[] tokens)
        {
            string cacheKey = this.CreateKey(key, tokens);
            var item = this.Cache.Get<T>(cacheKey);

            if (this.IsDefault(item))
            {
                object loadLock = this.GetLockObject(cacheKey, SyncLockDuration);
                lock (loadLock)
                {
                    item = this.Cache.Get<T>(cacheKey);
                    if (this.IsDefault(item))
                    {
                        item = fetchItemFunc();
                        this.Cache.Set(cacheKey, item, durationInSec);
                    }
                }
            }

            return item;
        }
    }
}
