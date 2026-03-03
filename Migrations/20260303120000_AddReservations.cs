using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SUMMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MobilityLocationId = table.Column<int>(type: "integer", nullable: false),
                    ReservationTime    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    City               = table.Column<string>(type: "text", nullable: false),
                    Type               = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name:       "FK_Reservations_MobilityLocations_MobilityLocationId",
                        column:     x => x.MobilityLocationId,
                        principalTable: "MobilityLocations",
                        principalColumn: "Id",
                        onDelete:   ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name:    "IX_Reservations_MobilityLocationId",
                table:   "Reservations",
                column:  "MobilityLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Reservations");
        }
    }
}
