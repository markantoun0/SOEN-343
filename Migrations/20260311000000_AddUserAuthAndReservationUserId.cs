using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SUMMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthAndReservationUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS so re-running is always safe.

            // 1. PasswordHash on Users
            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"PasswordHash\" text NOT NULL DEFAULT '';");

            // NOTE: StartDate and EndDate already existed in the Reservations table
            // before this migration was tracked — we intentionally skip them here.

            // 2. UserId (nullable FK) on Reservations
            migrationBuilder.Sql(
                "ALTER TABLE \"Reservations\" ADD COLUMN IF NOT EXISTS \"UserId\" integer NULL;");

            // 3. Index on UserId (IF NOT EXISTS requires Postgres 9.5+)
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Reservations_UserId\" ON \"Reservations\" (\"UserId\");");

            // 4. FK from Reservations.UserId → Users.Id
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_Reservations_Users_UserId'
    ) THEN
        ALTER TABLE ""Reservations""
            ADD CONSTRAINT ""FK_Reservations_Users_UserId""
            FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"") ON DELETE SET NULL;
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Reservations\" DROP CONSTRAINT IF EXISTS \"FK_Reservations_Users_UserId\";");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Reservations_UserId\";");

            migrationBuilder.Sql(
                "ALTER TABLE \"Reservations\" DROP COLUMN IF EXISTS \"UserId\";");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" DROP COLUMN IF EXISTS \"PasswordHash\";");
        }
    }
}
