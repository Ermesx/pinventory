using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class Change_OwnerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "pins",
                table: "TagCatalogs");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                schema: "pins",
                table: "TagCatalogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerId",
                schema: "pins",
                table: "TagCatalogs");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                schema: "pins",
                table: "Pins");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "pins",
                table: "TagCatalogs",
                type: "uuid",
                nullable: true);
        }
    }
}
