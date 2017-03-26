using Simple301.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Simple301.Core.Extensions;
using System.Text.RegularExpressions;
using Simple301.Core.Utilities;
using Simple301.Core.Utilities.Caching;

namespace Simple301.Core
{
    /// <summary>
    /// Redirect Repository that handles CRUD operations for the repository collection
    /// Utilizes Umbraco Database context to persist redirects into the database with
    /// cached in memory collection for fast query
    /// </summary>
    public static class RedirectRepository
    {
        private static CacheManager _cacheManager;
        private const int DEFAULT_CACHE_DURATION = 86400;
        private const string CACHE_CATEGORY = "Redirects";
        private const string CACHE_ALL_KEY = "AllRedirects";

        static RedirectRepository()
        {
            var settingsUtility = new SettingsUtility();

            // define the cache duration
            var cacheDuration = settingsUtility.AppSettingExists(SettingsKeys.CacheDurationKey) ?
                settingsUtility.GetAppSetting<int>(SettingsKeys.CacheDurationKey) : DEFAULT_CACHE_DURATION;

            // define cache enabled
            var cacheEnabled = settingsUtility.AppSettingExists(SettingsKeys.CacheEnabledKey) ?
                settingsUtility.GetAppSetting<bool>(SettingsKeys.CacheEnabledKey) :
                true;

            _cacheManager = new CacheManager(cacheDuration, cacheEnabled);
        }

        /// <summary>
        /// Get all redirects from the repositry
        /// </summary>
        /// <returns>Collection of redirects</returns>
        public static IEnumerable<Redirect> GetAllRedirects()
        {
            // Update with latest from DB
            return FetchRedirects().Select(x => x.Value);
        }

        /// <summary>
        /// Add a new redirect to the redirects collection
        /// </summary>
        /// <param name="oldUrl">Old Url to redirect from</param>
        /// <param name="newUrl">New Url to redirect to</param>
        /// <param name="notes">Any associated notes with this redirect</param>
        /// <returns>New redirect from DB if successful</returns>
        public static Redirect AddRedirect(bool isRegex, string oldUrl, string newUrl, string notes)
        {
            if (!oldUrl.IsSet()) throw new ArgumentNullException("oldUrl");
            if (!newUrl.IsSet()) throw new ArgumentNullException("newUrl");

            //Ensure starting slash if not regex
            if(!isRegex)
                oldUrl = oldUrl.EnsurePrefix("/").ToLower();

            // Allow external redirects and ensure slash if not absolute
            newUrl = Uri.IsWellFormedUriString(newUrl, UriKind.Absolute) ?
                newUrl : 
                newUrl.EnsurePrefix("/").ToLower();

            // First look for single match
            var redirect = FetchRedirectByOldUrl(oldUrl);
            if (redirect != null) throw new ArgumentException("A redirect for " + oldUrl + " already exists");

            // Second pull all for loop detection
            var redirects = FetchRedirects();
            if (!isRegex && DetectLoop(oldUrl, newUrl, redirects)) throw new ApplicationException("Adding this redirect would cause a redirect loop");

            //Add redirect to DB
            var db = ApplicationContext.Current.DatabaseContext.Database;
            var idObj = db.Insert(new Redirect()
            {
                IsRegex = isRegex,
                OldUrl = oldUrl,
                NewUrl = newUrl,
                LastUpdated = DateTime.Now.ToUniversalTime(),
                Notes = notes
            });

            //Clear the current cache
            ClearCache();

            //Fetch the added redirect
            var newRedirect = FetchRedirectById(Convert.ToInt32(idObj));

            //return new redirect
            return newRedirect;
        }

        /// <summary>
        /// Update a given redirect
        /// </summary>
        /// <param name="redirect">Redirect to update</param>
        /// <returns>Updated redirect if successful</returns>
        public static Redirect UpdateRedirect(Redirect redirect)
        {
            if (redirect == null) throw new ArgumentNullException("redirect");
            if (!redirect.OldUrl.IsSet()) throw new ArgumentNullException("redirect.OldUrl");
            if (!redirect.NewUrl.IsSet()) throw new ArgumentNullException("redirect.NewUrl");

            //Ensure starting slash
            if(!redirect.IsRegex)
                redirect.OldUrl = redirect.OldUrl.EnsurePrefix("/").ToLower();

            // Allow external redirects and ensure slash if not absolute
            redirect.NewUrl = Uri.IsWellFormedUriString(redirect.NewUrl, UriKind.Absolute) ?
                redirect.NewUrl :
                redirect.NewUrl.EnsurePrefix("/").ToLower();


            // First check if a single existing redirect
            var existingRedirect = FetchRedirectByOldUrl(redirect.OldUrl);
            if (existingRedirect != null && existingRedirect.Id != redirect.Id) throw new ArgumentException("A redirect for " + redirect.OldUrl + " already exists");

            // Second pull all for loop detection
            var redirects = FetchRedirects();
            if (!redirect.IsRegex && DetectLoop(redirect.OldUrl, redirect.NewUrl, redirects)) throw new ApplicationException("Adding this redirect would cause a redirect loop");

            //get DB Context, set update time, and persist
            var db = ApplicationContext.Current.DatabaseContext.Database;
            redirect.LastUpdated = DateTime.Now.ToUniversalTime();
            db.Update(redirect);

            //Clear the current cache
            ClearCache();

            //return updated redirect
            return redirect;
        }

        /// <summary>
        /// Handles deleting a redirect from the redirect collection
        /// </summary>
        /// <param name="id">Id of redirect to remove</param>
        public static void DeleteRedirect(int id)
        {
            var item = FetchRedirectById(id);
            if (item == null) throw new ArgumentException("No redirect with an Id that matches " + id);

            //Get database context and delete
            var db = ApplicationContext.Current.DatabaseContext.Database;
            db.Delete(item);

            //Clear the current cache
            ClearCache();
        }

        /// <summary>
        /// Handles finding a redirect based on the oldUrl
        /// </summary>
        /// <param name="oldUrl">Url to search for</param>
        /// <returns>Matched Redirect</returns>
        public static Redirect FindRedirect(string oldUrl)
        {
            var matchedRedirect = FetchRedirectByOldUrl(oldUrl, fromCache: true);
            if (matchedRedirect != null) return matchedRedirect;

            // fetch regex redirects
            var regexRedirects = FetchRegexRedirects(fromCache: true);

            foreach(var regexRedirect in regexRedirects)
            {
                if (Regex.IsMatch(oldUrl,regexRedirect.OldUrl)) return regexRedirect;
            }

            return null;
        }

        /// <summary>
        /// Handles clearing the cache
        /// </summary>
        public static void ClearCache()
        {
            // Delete all items in redirect category
            _cacheManager.GetCacheItems()
                .Where(x => x.Category.Equals(CACHE_CATEGORY))
                .ToList()
                .ForEach(x => _cacheManager.DeleteItem(x.Category, x.Key));
        }

        /// <summary>
        /// Fetches all redirects through cache layer
        /// </summary>
        /// <returns>Collection of redirects</returns>
        private static Dictionary<string,Redirect> FetchRedirects(bool fromCache = false)
        {
            // if from cache, make sure we add if it doesn't exist
            if (fromCache)
                return _cacheManager.GetAndSet(CACHE_CATEGORY, CACHE_ALL_KEY, () => FetchRedirectsFromDb());

            return FetchRedirectsFromDb();
        }

        /// <summary>
        /// Fetches a single redirect from the DB based on an Id
        /// </summary>
        /// <param name="id">Id of redirect to fetch</param>
        /// <returns>Single redirect with matching Id</returns>
        private static Redirect FetchRedirectById(int id, bool fromCache = false)
        {
            var query = "SELECT * FROM Redirects WHERE Id=@0";

            if (fromCache)
                return _cacheManager.GetAndSet(CACHE_CATEGORY, "Id:" + id, () => FetchRedirectFromDbByQuery(query, id));

            return FetchRedirectFromDbByQuery(query, id);
        }

        /// <summary>
        /// Fetches a single redirect from the DB based on OldUrl
        /// </summary>
        /// <param name="oldUrl">OldUrl of redirect to find</param>
        /// <returns>Single redirect with matching OldUrl</returns>
        private static Redirect FetchRedirectByOldUrl(string oldUrl, bool fromCache = false)
        {
            var query = "SELECT * FROM Redirects WHERE OldUrl=@0";

            if (fromCache)
                return _cacheManager.GetAndSet(CACHE_CATEGORY, "OldUrl:" + oldUrl, () => FetchRedirectFromDbByQuery(query, oldUrl));

            return FetchRedirectFromDbByQuery(query, oldUrl);
        }

        /// <summary>
        /// Fetches the list of Regex redirects from the DB or cache
        /// </summary>
        /// <param name="fromCache">Set to pull from cache</param>
        /// <returns>Collection or regex redirects</returns>
        private static List<Redirect> FetchRegexRedirects(bool fromCache = false)
        {
            var query = "SELECT * FROM Redirects WHERE IsRegex=@0";

            if (fromCache)
                return _cacheManager.GetAndSet(CACHE_CATEGORY, "RegexRedirects", () => FetchRedirectsFromDbByQuery(query, true));

            return FetchRedirectsFromDbByQuery(query, true);
        }

        /// <summary>
        /// Handles fetching a single redirect from the DB based
        /// on the provided query and parameter
        /// </summary>
        /// <typeparam name="T">Datatype of param</typeparam>
        /// <param name="query">Query</param>
        /// <param name="param">Param value</param>
        /// <returns>Redirect</returns>
        private static Redirect FetchRedirectFromDbByQuery<T>(string query, T param)
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            return db.FirstOrDefault<Redirect>(query, param);
        }

        /// <summary>
        /// Handles fetching a collection of redirects from the DB based
        /// on the provided query and parameter
        /// </summary>
        /// <typeparam name="T">Datatype of param</typeparam>
        /// <param name="query">Query</param>
        /// <param name="param">Param value</param>
        /// <returns>Collection of redirects</returns>
        private static List<Redirect> FetchRedirectsFromDbByQuery<T>(string query, T param)
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            return db.Query<Redirect>(query, param).ToList();
        }

        /// <summary>
        /// Fetches all redirects from the database
        /// </summary>
        /// <returns>Collection of redirects</returns>
        private static Dictionary<string, Redirect> FetchRedirectsFromDb()
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            var redirects = db.Query<Redirect>("SELECT * FROM Redirects");
            return redirects != null ? redirects.ToDictionary(x => x.OldUrl) : new Dictionary<string, Redirect>();
        }

        /// <summary>
        /// Detects a loop in the redirects list given the new redirect.
        /// Uses Floyd's cycle-finding algorithm.
        /// </summary>
        /// <param name="oldUrl">Old URL for new redirect</param>
        /// <param name="newUrl">New URL for new redirect</param>
        /// <param name="redirects">Current list of all redirects</param>
        /// <returns>True if loop detected, false if no loop detected</returns>
        private static bool DetectLoop(string oldUrl, string newUrl, Dictionary<string, Redirect> redirects)
        {
            // quick check for any links to this new redirect
            if (!redirects.ContainsKey(newUrl) && !redirects.Any(x => x.Value.NewUrl.Equals(oldUrl))) return false;

            // clone redirect list
            var linkedList = redirects.ToDictionary(entry => entry.Key, entry => entry.Value);
            var redirect = new Redirect() { OldUrl = oldUrl, NewUrl = newUrl };

            // add new redirect to cloned list for traversing
            if (!linkedList.ContainsKey(oldUrl))
                linkedList.Add(oldUrl, redirect);
            else
                linkedList[oldUrl] = redirect;

            // Use Floyd's cycle finding algorithm to detect loops in a linked list
            var slowP = redirect;
            var fastP = redirect;

            while (slowP != null && fastP != null && linkedList.ContainsKey(fastP.NewUrl))
            {
                slowP = linkedList[slowP.NewUrl];
                fastP = linkedList.ContainsKey(linkedList[fastP.NewUrl].NewUrl) ? linkedList[linkedList[fastP.NewUrl].NewUrl] : null;

                if (slowP == fastP)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
