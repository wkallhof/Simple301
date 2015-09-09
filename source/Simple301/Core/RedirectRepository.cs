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
        private static IEnumerable<Redirect> _redirects;

        static RedirectRepository()
        {
            _redirects = FetchRedirectsFromDb();
        }

        /// <summary>
        /// Get all redirects from the repositry
        /// </summary>
        /// <returns>Collection of redirects</returns>
        public static IEnumerable<Redirect> GetAll()
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
            if (_redirects.Any(x => x.OldUrl.Equals(oldUrl))) throw new ArgumentException("A redirect for " + oldUrl + " already exists");

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
            _redirects.ToList().Add(newRedirect);

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
            if (_redirects.Any(x => x.OldUrl.Equals(redirect.OldUrl) && x.Id != redirect.Id)) throw new ArgumentException("A redirect for " + redirect.OldUrl + " already exists");

            //get DB Context, set update time, and persist
            var db = ApplicationContext.Current.DatabaseContext.Database;
            redirect.LastUpdated = DateTime.Now.ToUniversalTime();
            db.Update(redirect);

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
            var item = _redirects.FirstOrDefault(x => x.Id.Equals(id));
            if (item == null) throw new ArgumentException("No redirect with an Id that matches " + id);

            //Get database context and delete
            var db = ApplicationContext.Current.DatabaseContext.Database;
            db.Delete(item);
        }

        /// <summary>
        /// Fetches all redirects from the database
        /// </summary>
        /// <returns>Collection of redirects</returns>
        private static IEnumerable<Redirect> FetchRedirectsFromDb()
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            var redirects = db.Query<Redirect>("SELECT * FROM Redirects");
            return redirects ?? new List<Redirect>();
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
