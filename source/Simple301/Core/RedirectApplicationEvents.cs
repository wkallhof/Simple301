using Semver;
using Simple301.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Web.Routing;
using System.Linq;
using Umbraco.Core.Persistence.Migrations;
using System;
using System.Web;
using Umbraco.Core.Services;

namespace Simple301.Core
{
    public class MyApplication : ApplicationEventHandler
    {
        private SemVersion _targetVersion = new SemVersion(1, 0, 1);
        private const string REDIRECTS_TABLE_NAME = "Redirects";
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
            var migrationService = ApplicationContext.Current.Services.MigrationEntryService;
            var creator = new DatabaseSchemaHelper(db, LoggerResolver.Current.Logger, SqlSyntaxContext.SqlSyntaxProvider);

            //Ensure the Redirects table exists. If not, create it
            if (!creator.TableExist(REDIRECTS_TABLE_NAME))
            {
                creator.CreateTable<Redirect>(false);
                this.AddTargetVersionMigrationEntry(migrationService);
            }
            else if(!MigrationVersionCheck(migrationService) && !ValidateTableRead(db, migrationService))
            {
                HandleMigrations(db, migrationService);
            }
        }

        /// <summary>
        /// Check if we have an entry for the latest version
        /// </summary>
        /// <param name="migrationService">Migration service</param>
        /// <returns>True if we have an entry</returns>
        private bool MigrationVersionCheck(IMigrationEntryService migrationService)
        {
            return migrationService.FindEntry(REDIRECTS_TABLE_NAME, this._targetVersion) != null;
        }

        /// <summary>
        /// Helper method called to update the migration entries with an entry that matches the
        /// latest target version
        /// </summary>
        /// <param name="migrationService"></param>
        private void AddTargetVersionMigrationEntry(IMigrationEntryService migrationService)
        {
            var match = migrationService.FindEntry(REDIRECTS_TABLE_NAME, this._targetVersion);
            if (match == null)
                migrationService.CreateEntry(REDIRECTS_TABLE_NAME, this._targetVersion);
        }

        /// <summary>
        /// Hack to support legacy version checking. This supports the state that exists before
        /// we implemented migration version entry on database create, which will exist moving forward.
        /// This only supports legacy installs
        /// </summary>
        /// <param name="db">Database</param>
        /// <param name="migrationService">Migration service</param>
        /// <returns>True if success</returns>
        private bool ValidateTableRead(UmbracoDatabase db, IMigrationEntryService migrationService)
        {
            try
            {
                // run through creating and deleting a redirect.
                var redirect = RedirectRepository.AddRedirect(true, "old", "new", "notes");
                RedirectRepository.DeleteRedirect(redirect.Id);
                this.AddTargetVersionMigrationEntry(migrationService);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        /// <summary>
        /// Handles checking and running migrations
        /// </summary>
        /// <param name="db">Database context</param>
        /// <param name="migrationService">Migration service</param>
        private void HandleMigrations(UmbracoDatabase db, IMigrationEntryService migrationService)
        {
            var latestMigrationVersion = new SemVersion(0, 0, 0);

            // get all migrations for "Redirects" already executed
            var migrations = migrationService.GetAll(REDIRECTS_TABLE_NAME);

            // get the latest migration for "Redirects" executed
            var latestMigration = migrations.OrderByDescending(x => x.Version).FirstOrDefault();

            if (latestMigration != null)
                latestMigrationVersion = latestMigration.Version;

            if (this._targetVersion == latestMigrationVersion)
                return;

            var migrationsRunner = new MigrationRunner(
              migrationService,
              ApplicationContext.Current.ProfilingLogger.Logger,
              latestMigrationVersion,
              this._targetVersion,
              REDIRECTS_TABLE_NAME);

            try
            {
                migrationsRunner.Execute(db);
            }
            catch (HttpException e){}
            catch (Exception e)
            {
                LogHelper.Error<MigrationRunner>("Error running Redirects migration", e);
            }
        }
    }
}
