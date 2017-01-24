using Simple301.Core.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace Simple301.Core.Utilities.Caching
{
    /// <summary>
    /// Cache Manager service that provides context and control
    /// over the HttpContext.Current.Cache
    /// </summary>
    public class CacheManager
    {
        //Delimiter for the Category and Key names
        private const char CATEGORY_KEY_DELIMITER = '$';

        //Property representing the Status of the CacheManager
        public CacheManagerStatus Status;

        public CacheManager(int cacheDuration, bool cacheEnabled)
        {
            //Set the status on site spinup, reading from AppSettings
            this.Status = new CacheManagerStatus()
            {
                CacheDuration = cacheDuration,
                CacheEnabled = cacheEnabled
            };
        }

        /// <summary>
        /// Gets and item from the cache for the given key
        /// as the supplied type
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="key">Cache key for lookup</param>
        /// <returns>Cached item as T</returns>
        public virtual T GetItem<T>(string category, string key) where T : class
        {
            //Fetch the lookup key
            var lookupKey = this.FetchLookupKey(category, key);
            if (!lookupKey.IsSet()) return null;

            //Get the item
            return HttpContext.Current.Cache.Get(lookupKey) as T;
        }

        /// <summary>
        /// Deletes a cached item given the category and key
        /// for the item
        /// </summary>
        /// <param name="category">Category of item to delete</param>
        /// <param name="key">Key of item to delete</param>
        public void DeleteItem(string category, string key)
        {
            //Fetch the lookup Key
            var lookupKey = this.FetchLookupKey(category, key);
            if (!lookupKey.IsSet()) return;

            //Remove from cache
            HttpContext.Current.Cache.Remove(lookupKey);
        }

        /// <summary>
        /// Method that checks for the existence of the a cached
        /// item and if it doesn't exist, run the provided
        /// setFunction to get the model to cache.
        /// </summary>
        /// <typeparam name="T">Type of item to return</typeparam>
        /// <param name="category">Category for lookup</param>
        /// <param name="key">Key for lookup</param>
        /// <param name="setFunction">Function to call if item not found</param>
        /// <returns>Found item, from cache or function</returns>
        public virtual T GetAndSet<T>(string category, string key, Func<T> setFunction) where T : class
        {
            //Fetch lookup key
            var lookupKey = this.FetchLookupKey(category, key);
            if (!lookupKey.IsSet()) return setFunction.Invoke();

            //Check and get item
            var item = HttpContext.Current.Cache.Get(lookupKey) as T;
            if (item == null)
            {
                //Item not found, run the function and cache the result
                item = setFunction.Invoke();
                if (item != null)
                    this.AddItem<T>(category, key, item);
            }

            //return cached item
            return item;
        }

        /// <summary>
        /// Adds an item to the cache at the given key
        /// </summary>
        /// <typeparam name="T">Type to store</typeparam>
        /// <param name="key">Key to store for lookup</param>
        /// <param name="value">Value to store in cache</param>
        public virtual void AddItem<T>(string category, string key, T value) where T : class
        {
            //If caching isn't enabled, don't cache
            if (!Status.CacheEnabled) return;

            //Fetch lookup key
            var lookupKey = this.FetchLookupKey(category, key);
            if (!lookupKey.IsSet()) return;

            //Add to the cache
            var expiration = DateTime.Now.AddSeconds(this.Status.CacheDuration);
            HttpContext.Current.Cache.Insert(lookupKey, value, null, expiration, Cache.NoSlidingExpiration);
        }

        /// <summary>
        /// Handles fetching the cache lookup tree. Reads
        /// the current cache and looks for the items we added.
        /// </summary>
        /// <returns>Collection of Cache Items</returns>
        public List<CacheItem> GetCacheItems()
        {
            var list = new List<CacheItem>();

            //Foreach item in the HttpContext.Current.Cache, return the ones
            //that match our naming convetion
            foreach (var item in HttpContext.Current.Cache)
            {
                var key = ((DictionaryEntry)item).Key.ToString();
                var split = key.Split(CATEGORY_KEY_DELIMITER);
                if (split.Length == 2)
                {
                    list.Add(new CacheItem() { Category = split[0], Key = split[1] });
                }
            }

            return list;
        }

        /// <summary>
        /// Helper method used to define the cache key
        /// given a category and key
        /// </summary>
        /// <param name="category">Category to use for key</param>
        /// <param name="key">Key string ot use with Category</param>
        /// <returns>Return Category + Key combo</returns>
        private string FetchLookupKey(string category, string key)
        {
            if (!category.IsSet() || !key.IsSet()) return string.Empty;

            return category.Replace(" ", "") + CATEGORY_KEY_DELIMITER + key.Replace(" ", "");
        }
    }
}
