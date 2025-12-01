#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class ImproveSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlacesProcessed",
                schema: "pins",
                table: "ImportProcesses");

            migrationBuilder.RenameColumn(
                name: "TotalPlaces",
                schema: "pins",
                table: "ImportProcesses",
                newName: "BatchesToProceed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BatchesToProceed",
                schema: "pins",
                table: "ImportProcesses",
                newName: "TotalPlaces");

            migrationBuilder.AddColumn<int>(
                name: "PlacesProcessed",
                schema: "pins",
                table: "ImportProcesses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}