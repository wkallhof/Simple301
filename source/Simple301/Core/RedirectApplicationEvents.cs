using Simple301.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Web.Routing;

namespace Simple301.Core
{
    public class MyApplication : ApplicationEventHandler
    {
        /// <summary>
        /// On application starting we inject the Redirect Content Finder into the very
        /// first slot of Umbraco's Content Resolver pipeline
        /// </summary>
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentFinderResolver.Current.InsertType<RedirectContentFinder>(0);
        }

        /// <summary>
        /// On Application Started we need to ensure that the Redirects table exists. If not, create it
        /// </summary>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //Grab the Umbraco database context and spin up a new DatabaseSchemaHelper
            var db = applicationContext.DatabaseContext.Database;
            var creator = new DatabaseSchemaHelper(db, LoggerResolver.Current.Logger, SqlSyntaxContext.SqlSyntaxProvider);

            //Ensure the Redirects table exists. If not, create it
            if (!creator.TableExist("Redirects"))
                creator.CreateTable<Redirect>(false);
        }
    }
}
