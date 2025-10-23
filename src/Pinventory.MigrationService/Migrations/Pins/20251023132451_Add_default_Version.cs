using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class Add_default_Version : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "pins",
                table: "TagCatalogs",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "pins",
                table: "Pins",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "pins",
                table: "TagCatalogs",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "pins",
                table: "Pins",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 0L);
        }
    }
}