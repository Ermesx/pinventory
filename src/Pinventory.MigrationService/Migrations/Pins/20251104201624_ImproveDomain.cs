#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class ImproveDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AddedAt",
                schema: "pins",
                table: "Pins",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ImportConflictedPlaces",
                schema: "pins",
                columns: table => new
                {
                    MapsUrl = table.Column<string>(type: "text", nullable: false),
                    AddedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportConflictedPlaces", x => new { x.ImportId, x.MapsUrl, x.AddedDate });
                    table.ForeignKey(
                        name: "FK_ImportConflictedPlaces_Imports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "pins",
                        principalTable: "Imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportFailedPlaces",
                schema: "pins",
                columns: table => new
                {
                    MapsUrl = table.Column<string>(type: "text", nullable: false),
                    AddedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportFailedPlaces", x => new { x.ImportId, x.MapsUrl, x.AddedDate });
                    table.ForeignKey(
                        name: "FK_ImportFailedPlaces_Imports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "pins",
                        principalTable: "Imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportConflictedPlaces",
                schema: "pins");

            migrationBuilder.DropTable(
                name: "ImportFailedPlaces",
                schema: "pins");

            migrationBuilder.DropColumn(
                name: "AddedAt",
                schema: "pins",
                table: "Pins");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "pins",
                table: "Pins");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "pins",
                table: "Pins");
        }
    }
}