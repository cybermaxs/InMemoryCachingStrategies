using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace InMemoryCachingStrategies
{
    internal class BaseStrategy
    {
        private const string TokenSeparator = "¤";
        private const string SyncLockObjectCacheKeyPrefix = "¤lock¤";
        private static ConcurrentDictionary<string, object> lockDictionary = new ConcurrentDictionary<string, object>();
        private static readonly object SyncLock = new object();

        protected InMemoryCache Cache { get; set; }
        protected BaseStrategy(InMemoryCache cache)
        {
            this.Cache = cache;
        }

        /// <summary>
        /// Generate a cache key by concatening key and token.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        protected string CreateKey(string key, params string[] tokens)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            return tokens.Length == 0 ? key : key + TokenSeparator + string.Join(TokenSeparator, tokens);
        }

        /// <summary>
        /// Tell is item is default(T)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        protected bool IsDefault<T>(T item)
        {
            return EqualityComparer<T>.Default.Equals(item, default(T));
        }

        protected object GetLockObject(string key, int? syncLockDuration=null)
        {
            object lockobject;
            if (syncLockDuration.HasValue)
            {
                //limited lock lifetime
                string syncLockKey = SyncLockObjectCacheKeyPrefix + key;
                lock (SyncLock)
                {
                    lockobject = this.Cache.Get<object>(syncLockKey);
                    if (lockobject == null)
                    {
                        lockobject = new object();
                        this.Cache.Set(syncLockKey, lockobject, syncLockDuration.Value);
                    }
                    return lockobject;
                }
            }
            //shared lock dictionary
            lockobject = lockDictionary.AddOrUpdate(key, k => new object(), (k, old) => old);
            return lockobject;
        }
    }
}
