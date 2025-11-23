#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class FixVersionForSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "pins",
                table: "ImportProcesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "pins",
                table: "ImportProcesses",
                type: "integer",
                rowVersion: true,
                nullable: false);
        }
    }
}