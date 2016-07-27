using Simple301.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Simple301.Core.Extensions;

namespace Simple301.Core
{
    /// <summary>
    /// Redirect Repository that handles CRUD operations for the repository collection
    /// Utilizes Umbraco Database context to persist redirects into the database but
    /// utilizes an in-memory collection for fast querying. 
    /// </summary>
    public static class RedirectRepository
    {
        private static Dictionary<string,Redirect> _redirects;

        static RedirectRepository()
        {
            _redirects = FetchRedirectsFromDb();
        }

        /// <summary>
        /// Get all redirects from the repositry
        /// </summary>
        /// <returns>Collection of redirects</returns>
        public static IEnumerable<Redirect> GetAllRedirects()
        {
            return _redirects.Select(x => x.Value);
        }

        /// <summary>
        /// Get the lookup table for quick lookups
        /// </summary>
        /// <returns>Dictionary of OldUrl => Redirect </returns>
        public static Dictionary<string, Redirect> GetLookupTable()
        {
            return _redirects;
        }

        /// <summary>
        /// Add a new redirect to the redirects collection
        /// </summary>
        /// <param name="oldUrl">Old Url to redirect from</param>
        /// <param name="newUrl">New Url to redirect to</param>
        /// <param name="notes">Any associated notes with this redirect</param>
        /// <returns>New redirect from DB if successful</returns>
        public static Redirect AddRedirect(string oldUrl, string newUrl, string notes)
        {
            if (!oldUrl.IsSet()) throw new ArgumentNullException("oldUrl");
            if (!newUrl.IsSet()) throw new ArgumentNullException("newUrl");

            //Ensure starting slash
            oldUrl = oldUrl.EnsurePrefix("/").ToLower();

            // Allow external redirects and ensure slash if not absolute
            newUrl = Uri.IsWellFormedUriString(newUrl, UriKind.Absolute) ?
                newUrl.ToLower() : 
                newUrl.EnsurePrefix("/").ToLower();

            if (_redirects.ContainsKey(oldUrl)) throw new ArgumentException("A redirect for " + oldUrl + " already exists");
            if (DetectLoop(oldUrl, newUrl)) throw new ApplicationException("Adding this redirect would cause a redirect loop");

            //Add redirect to DB
            var db = ApplicationContext.Current.DatabaseContext.Database;
            var idObj = db.Insert(new Redirect()
            {
                OldUrl = oldUrl,
                NewUrl = newUrl,
                LastUpdated = DateTime.Now.ToUniversalTime(),
                Notes = notes
            });

            //Fetch the added redirect to put into the in-memory collection
            var newRedirect = FetchRedirectById(Convert.ToInt32(idObj));
            _redirects[newRedirect.OldUrl] = newRedirect;

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
            redirect.OldUrl = redirect.OldUrl.EnsurePrefix("/").ToLower();

            // Allow external redirects and ensure slash if not absolute
            redirect.NewUrl = Uri.IsWellFormedUriString(redirect.NewUrl, UriKind.Absolute) ?
                redirect.NewUrl.ToLower() :
                redirect.NewUrl.EnsurePrefix("/").ToLower();

            var existingRedirect = _redirects.ContainsKey(redirect.OldUrl) ? _redirects[redirect.OldUrl] : null;
            if (existingRedirect != null && existingRedirect.Id != redirect.Id) throw new ArgumentException("A redirect for " + redirect.OldUrl + " already exists");
            if (DetectLoop(redirect.OldUrl, redirect.NewUrl)) throw new ApplicationException("Adding this redirect would cause a redirect loop");

            //get DB Context, set update time, and persist
            var db = ApplicationContext.Current.DatabaseContext.Database;
            redirect.LastUpdated = DateTime.Now.ToUniversalTime();
            db.Update(redirect);

            //Update in-memory list
            _redirects[redirect.OldUrl] = redirect;

            //return updated redirect
            return redirect;
        }

        /// <summary>
        /// Handles deleting a redirect from the redirect collection
        /// </summary>
        /// <param name="id">Id of redirect to remove</param>
        public static void DeleteRedirect(int id)
        {
            //Look for the redirect that has a matching Id
            var item = _redirects.Values.FirstOrDefault(x => x.Id.Equals(id));
            if (item == null) throw new ArgumentException("No redirect with an Id that matches " + id);

            //Get database context and delete
            var db = ApplicationContext.Current.DatabaseContext.Database;
            db.Delete(item);

            //Update in-memory list
            _redirects.Remove(item.OldUrl);
        }

        /// <summary>
        /// Fetches all redirects from the database
        /// </summary>
        /// <returns>Collection of redirects</returns>
        private static Dictionary<string,Redirect> FetchRedirectsFromDb()
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            var redirects = db.Query<Redirect>("SELECT * FROM Redirects");
            return redirects != null ? redirects.ToDictionary(x => x.OldUrl) : new Dictionary<string, Redirect>();
        }

        /// <summary>
        /// Fetches a single redirect from the DB based on an Id
        /// </summary>
        /// <param name="id">Id of redirect to fetch</param>
        /// <returns>Single redirect with matching Id</returns>
        private static Redirect FetchRedirectById(int id)
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            return db.FirstOrDefault<Redirect>("SELECT * FROM Redirects WHERE Id=@0", id);
        }

        /// <summary>
        /// Detects a loop in the redirects list given the new redirect.
        /// Uses Floyd's cycle-finding algorithm.
        /// </summary>
        /// <param name="oldUrl">Old URL for new redirect</param>
        /// <param name="newUrl">New URL for new redirect</param>
        /// <returns>True if loop detected, false if no loop detected</returns>
        private static bool DetectLoop(string oldUrl, string newUrl)
        {
            // quick check for any links to this new redirect
            if (!_redirects.ContainsKey(newUrl) && !_redirects.Any(x => x.Value.NewUrl.Equals(oldUrl))) return false;

            // clone redirect list
            var linkedList = _redirects.ToDictionary(entry => entry.Key, entry => entry.Value);
            var redirect = new Redirect() { OldUrl = oldUrl, NewUrl = newUrl };

            // add new redirect to cloned list for traversing
            if (!linkedList.ContainsKey(oldUrl))
                linkedList.Add(oldUrl, redirect);

            // Use Floyd's cycle finding algorithm to detect loops in a linked list
            var slowP = redirect;
            var fastP = redirect;

            while (slowP != null && fastP != null && linkedList.ContainsKey(fastP.NewUrl))
            {
                slowP = linkedList[slowP.NewUrl];
                fastP = linkedList[linkedList[fastP.NewUrl].NewUrl];

                if (slowP == fastP)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
