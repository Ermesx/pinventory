#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class AddPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodEnd",
                schema: "pins",
                table: "Imports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodStart",
                schema: "pins",
                table: "Imports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                schema: "pins",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                schema: "pins",
                table: "Imports");
        }
    }
}