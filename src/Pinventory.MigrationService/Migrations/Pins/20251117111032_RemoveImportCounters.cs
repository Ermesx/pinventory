#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class RemoveImportCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Conflicts",
                schema: "pins",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "Created",
                schema: "pins",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "Failed",
                schema: "pins",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "Processed",
                schema: "pins",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "Total",
                schema: "pins",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "Updated",
                schema: "pins",
                table: "Imports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Conflicts",
                schema: "pins",
                table: "Imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Created",
                schema: "pins",
                table: "Imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Failed",
                schema: "pins",
                table: "Imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Processed",
                schema: "pins",
                table: "Imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Total",
                schema: "pins",
                table: "Imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Updated",
                schema: "pins",
                table: "Imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}