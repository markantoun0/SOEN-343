using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SUMMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PreferredCity"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PreferredMobilityType"" text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" DROP COLUMN IF EXISTS ""PreferredCity"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" DROP COLUMN IF EXISTS ""PreferredMobilityType"";");
        }
    }
}
