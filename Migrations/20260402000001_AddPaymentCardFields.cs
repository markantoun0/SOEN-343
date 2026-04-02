using Microsoft.EntityFrameworkCore.Migrations;

namespace SUMMS.Api.Migrations;

/// <inheritdoc />
public partial class AddPaymentCardFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CardNumber",
            table: "Payments",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExpiryDate",
            table: "Payments",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PaymentDate",
            table: "Payments",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CardNumber",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "ExpiryDate",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "PaymentDate",
            table: "Payments");
    }
}

