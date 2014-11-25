using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace InMemoryCachingStrategies
{
    /// <summary>
    /// A basic System.Runtime.Caching.MemoryCache wrapper.
    /// </summary>
    public class InMemoryCache
    {
        private const string TokenSeparator = "¤";
        private const string SyncLockObjectCacheKeyPrefix = "¤lock¤";

        private MemoryCache _cache;
        public InMemoryCache()
        {
            _cache = new MemoryCache(Guid.NewGuid().ToString(), new System.Collections.Specialized.NameValueCollection() { { "pollingInterval", "00:00:01" } });
        }

        public long Count
        {
            get
            {
                return _cache.Count();
            }
        }

        private static object syncLock = new object();

        public bool Exists(string key)
        {
            return _cache[key] != null;
        }

        public long Flush()
        {
            return _cache.Trim(100);
        }

        /// <summary>
        /// Gets an item of type T from local cache
        /// </summary>
        public T Get<T>(string key)
        {
            var o = _cache[key];
            if (o == null) return default(T);
            if (o is T)
                return (T)o;
            return default(T);
        }

        /// <summary>
        /// Places an item of type T into local cache for the specified duration
        /// </summary>
        public void Set<T>(string key, T value, int? durationSecs, bool sliding = false)
        {
            SetWithPriority<T>(key, value, durationSecs, sliding, CacheItemPriority.Default);
        }

        public void SetWithPriority<T>(string key, T value, int? durationSecs, bool isSliding, CacheItemPriority priority)
        {
            RawSet(key, value, durationSecs, isSliding, priority);
        }

        private void RawSet(string cacheKey, object value, int? durationSecs, bool isSliding, CacheItemPriority priority)
        {
            var policy = new CacheItemPolicy { Priority = priority };
            if (!isSliding && durationSecs.HasValue)
                policy.AbsoluteExpiration = DateTime.UtcNow.AddSeconds(durationSecs.Value);
            if (isSliding && durationSecs.HasValue)
                policy.SlidingExpiration = TimeSpan.FromSeconds(durationSecs.Value);

            _cache.Set(cacheKey, value, policy);
        }

        /// <summary>
        /// Removes an item from local cache
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        //inspired from http://redis.io/commands/setnx
        public bool SetNXSync<T>(string key, T val)
        {
            lock (syncLock)
            {
                var item = this.Get<T>(key);
                if (EqualityComparer<T>.Default.Equals(item, default(T)))
                {
                    this.Set<T>(key, val, null, false);
                    return true;
                }
                return false;
            }
        }
    }
}
