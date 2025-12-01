#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class ImportImprovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportConflictedPlaces",
                schema: "pins");

            migrationBuilder.DropTable(
                name: "ImportFailedPlaces",
                schema: "pins");

            migrationBuilder.AlterColumn<int>(
                name: "Total",
                schema: "pins",
                table: "Imports",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                schema: "pins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false), ImportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportBatches_Imports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "pins",
                        principalTable: "Imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportStarredPlaces",
                schema: "pins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    GoogleMapsUrl = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    AddedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportStarredPlaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportStarredPlaces_ImportBatches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "pins",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ImportId",
                schema: "pins",
                table: "ImportBatches",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportStarredPlaces_BatchId",
                schema: "pins",
                table: "ImportStarredPlaces",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportStarredPlaces_State",
                schema: "pins",
                table: "ImportStarredPlaces",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportStarredPlaces",
                schema: "pins");

            migrationBuilder.DropTable(
                name: "ImportBatches",
                schema: "pins");

            migrationBuilder.AlterColumn<long>(
                name: "Total",
                schema: "pins",
                table: "Imports",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "ImportConflictedPlaces",
                schema: "pins",
                columns: table => new
                {
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapsUrl = table.Column<string>(type: "text", nullable: false),
                    AddedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapsUrl = table.Column<string>(type: "text", nullable: false),
                    AddedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
    }
}