using Semver;
using Simple301.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Web.Routing;
using System.Linq;
using Umbraco.Core.Persistence.Migrations;
using Umbraco.Web;
using System;
using System.Web;

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
            else
            {
                HandleMigrations();
            }
        }

        private static void HandleMigrations()
        {
            const string productName = "Redirects";
            var currentVersion = new SemVersion(0, 0, 0);

            // get all migrations for "Statistics" already executed
            var migrations = ApplicationContext.Current.Services.MigrationEntryService.GetAll(productName);

            // get the latest migration for "Statistics" executed
            var latestMigration = migrations.OrderByDescending(x => x.Version).FirstOrDefault();

            if (latestMigration != null)
                currentVersion = latestMigration.Version;

            var targetVersion = new SemVersion(1, 0, 1);
            if (targetVersion == currentVersion)
                return;

            var migrationsRunner = new MigrationRunner(
              ApplicationContext.Current.Services.MigrationEntryService,
              ApplicationContext.Current.ProfilingLogger.Logger,
              currentVersion,
              targetVersion,
              productName);

            try
            {
                migrationsRunner.Execute(UmbracoContext.Current.Application.DatabaseContext.Database);
            }
            catch (HttpException e){}
            catch (Exception e)
            {
                LogHelper.Error<MigrationRunner>("Error running Redirects migration", e);
            }
        }
    }
}
