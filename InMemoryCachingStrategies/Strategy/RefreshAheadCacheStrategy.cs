using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryCachingStrategies.Strategy
{
    internal class RefreshAheadCacheStrategy : BaseStrategy, ICacheStrategy
    {
        private float staleRatio = 1F;
        public RefreshAheadCacheStrategy(InMemoryCache cache = null, float staleRatio = 1F)
            : base(cache ?? new InMemoryCache())
        {
            this.staleRatio = staleRatio;
        }

        private string GetRefreshKey(string cacheKey)
        {
            return cacheKey + "-refresh";
        }

        private bool GetRefreshLock(string competeKey)
        {
            if (!this.Cache.SetNXSync(competeKey, DateTime.UtcNow))
            {
                var x = this.Cache.Get<DateTime>(competeKey);
                // Somebody abandoned the lock, clear it and try again
                if (DateTime.UtcNow - x > TimeSpan.FromMinutes(5))
                {
                    this.Cache.Remove(competeKey);
                    return GetRefreshLock(competeKey);
                }
                return false;
            }
            return true;
        }

        public T Get<T>(string key, Func<T> fetchItemFunc, int durationInSec, params string[] tokens)
        {
            var cachekey = this.CreateKey(key, tokens);
            var item = this.Cache.Get<DataCacheItem<T>>(cachekey);

            var loadLock = this.GetLockObject(cachekey);

            if (this.IsDefault(item))
            {
                lock (loadLock)
                {
                    // See if we have the value cached
                    item = this.Cache.Get<DataCacheItem<T>>(cachekey);
                    if (this.IsDefault(item))
                    {
                        // No data, run this synchronously to get data
                        var result = fetchItemFunc();
                        this.Cache.Set(cachekey, new DataCacheItem<T>(result, durationInSec), durationInSec + (int)staleRatio * durationInSec);
                        return result;
                    }
                }
            }

            // not stale or don't use refresh ahead, nothing else to do => back to double lock strategy
            if (!item.IsStale || staleRatio == 0) return item.DataItem;
            // Oh no, we're stale - kick off a background refresh

            var refreshLockSuccess = false;
            var refreshKey = GetRefreshKey(cachekey);
            if (Monitor.TryEnter(loadLock, 0))
            {
                try
                {
                    refreshLockSuccess = GetRefreshLock(refreshKey);
                }
                finally
                {
                    Monitor.Exit(loadLock);
                }
            }

            if (refreshLockSuccess)
            {
                var task = new Task(() =>
                {
                    lock (loadLock)
                    {
                        try
                        {
                            if (!item.IsStale) return;
                            if (item.IsUpdating) return;
                            item.IsUpdating = true;

                            var result = fetchItemFunc();
                            this.Cache.Set(cachekey, new DataCacheItem<T>(result, durationInSec), durationInSec + (int)staleRatio * durationInSec);
                        }
                        finally
                        {
                            this.Cache.Remove(refreshKey);
                            item.IsUpdating = false;
                        }
                    }
                });
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted) Trace.WriteLine(t.Exception);
                });
                task.Start();
            }
            return item.DataItem;
        }
    }

    /// <summary>
    /// A data container for a cache item..
    /// </summary>
    public class DataCacheItem<T> : DataCacheItem
    {
        public T DataItem { get; set; }

        public DataCacheItem(T dataItem, int cacheForSeconds)
            : base(cacheForSeconds)
        {
            this.DataItem = dataItem;
        }
    }

    /// <summary>
    /// A base class foreach cache item.
    /// </summary>
    public class DataCacheItem
    {
        internal bool IsUpdating = false;

        public bool IsStale
        {
            get
            {
                return this.StaleAt < DateTime.UtcNow;
            }
        }

        public DateTime StaleAt { get; private set; }

        public DataCacheItem(int cacheForSeconds)
        {
            this.StaleAt = DateTime.UtcNow.AddSeconds(cacheForSeconds);
        }
    }
}
