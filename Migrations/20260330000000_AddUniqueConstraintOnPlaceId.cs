using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SUMMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintOnPlaceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, identify and remove duplicate PlaceIds, keeping only the one with the most recent ID
            // This handles any existing data corruption
            migrationBuilder.Sql(
                @"DELETE FROM ""MobilityLocations"" ml1
                  WHERE ""Id"" NOT IN (
                    SELECT MAX(""Id"")
                    FROM ""MobilityLocations"" ml2
                    GROUP BY ""PlaceId""
                  );");

            // Now create the unique index on PlaceId
            migrationBuilder.CreateIndex(
                name: "IX_MobilityLocations_PlaceId",
                table: "MobilityLocations",
                column: "PlaceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MobilityLocations_PlaceId",
                table: "MobilityLocations");
        }
    }
}

