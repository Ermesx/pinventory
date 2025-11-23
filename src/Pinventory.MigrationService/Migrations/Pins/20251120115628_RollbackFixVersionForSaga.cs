#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class RollbackFixVersionForSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "pins",
                table: "ImportProcesses",
                type: "integer",
                defaultValue: 0L,
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "pins",
                table: "ImportProcesses");
        }
    }
}