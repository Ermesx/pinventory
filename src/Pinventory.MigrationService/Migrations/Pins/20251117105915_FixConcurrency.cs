#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class FixConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Version",
                schema: "pins",
                table: "TagCatalogs",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "Version",
                schema: "pins",
                table: "Pins",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "Version",
                schema: "pins",
                table: "Imports",
                newName: "xmin");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                schema: "pins",
                table: "TagCatalogs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 0L);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                schema: "pins",
                table: "Pins",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 0L);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                schema: "pins",
                table: "Imports",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "xmin",
                schema: "pins",
                table: "TagCatalogs",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "xmin",
                schema: "pins",
                table: "Pins",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "xmin",
                schema: "pins",
                table: "Imports",
                newName: "Version");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "pins",
                table: "TagCatalogs",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "pins",
                table: "Pins",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "pins",
                table: "Imports",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);
        }
    }
}