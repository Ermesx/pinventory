using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pins");

            migrationBuilder.CreateTable(
                name: "Pins",
                schema: "pins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaceId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StatusUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagCatalogs",
                schema: "pins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PinTags",
                schema: "pins",
                columns: table => new
                {
                    Value = table.Column<string>(type: "text", nullable: false),
                    PinId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinTags", x => new { x.PinId, x.Value });
                    table.ForeignKey(
                        name: "FK_PinTags_Pins_PinId",
                        column: x => x.PinId,
                        principalSchema: "pins",
                        principalTable: "Pins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogTags",
                schema: "pins",
                columns: table => new
                {
                    Value = table.Column<string>(type: "text", nullable: false),
                    CatalogId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogTags", x => new { x.CatalogId, x.Value });
                    table.ForeignKey(
                        name: "FK_CatalogTags_TagCatalogs_CatalogId",
                        column: x => x.CatalogId,
                        principalSchema: "pins",
                        principalTable: "TagCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogTags_Value",
                schema: "pins",
                table: "CatalogTags",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_Pins_PlaceId",
                schema: "pins",
                table: "Pins",
                column: "PlaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PinTags_Value",
                schema: "pins",
                table: "PinTags",
                column: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogTags",
                schema: "pins");

            migrationBuilder.DropTable(
                name: "PinTags",
                schema: "pins");

            migrationBuilder.DropTable(
                name: "TagCatalogs",
                schema: "pins");

            migrationBuilder.DropTable(
                name: "Pins",
                schema: "pins");
        }
    }
}
