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
            newUrl = newUrl.EnsurePrefix("/").ToLower();

            if (_redirects.ContainsKey(oldUrl)) throw new ArgumentException("A redirect for " + oldUrl + " already exists");

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
            redirect.NewUrl = redirect.NewUrl.EnsurePrefix("/").ToLower();

            var existingRedirect = _redirects.ContainsKey(redirect.OldUrl) ? _redirects[redirect.OldUrl] : null;
            if (existingRedirect != null && existingRedirect.Id != redirect.Id) throw new ArgumentException("A redirect for " + redirect.OldUrl + " already exists");

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
    }
}
