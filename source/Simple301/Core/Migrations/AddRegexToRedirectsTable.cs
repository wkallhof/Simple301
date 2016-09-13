using Umbraco.Core.Logging;
using Umbraco.Core.Persistence.Migrations;
using Umbraco.Core.Persistence.SqlSyntax;

namespace Example.Migrations
{
    [Migration("1.0.1", 1, "Redirects")]
    public class AddRegexToRedirectsTable : MigrationBase
    {
        public AddRegexToRedirectsTable(ISqlSyntaxProvider sqlSyntax, ILogger logger)
          : base(sqlSyntax, logger)
        { }

        public override void Up()
        {
            Alter.Table("Redirects").AddColumn("IsRegex").AsBoolean().Nullable();
        }

        public override void Down()
        {
            Delete.Column("IsRegex").FromTable("Redirects");
        }
    }
}
