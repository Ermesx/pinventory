#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class RemoveOwnBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""TRUNCATE TABLE pins."ImportStarredPlaces";""");

            migrationBuilder.Sql("""DROP VIEW IF EXISTS pins."ImportSummaries";""");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportStarredPlaces_ImportBatches_BatchId",
                schema: "pins",
                table: "ImportStarredPlaces");

            migrationBuilder.DropTable(
                name: "ImportBatches",
                schema: "pins");

            migrationBuilder.RenameColumn(
                name: "BatchId",
                schema: "pins",
                table: "ImportStarredPlaces",
                newName: "ImportId");

            migrationBuilder.RenameIndex(
                name: "IX_ImportStarredPlaces_BatchId",
                schema: "pins",
                table: "ImportStarredPlaces",
                newName: "IX_ImportStarredPlaces_ImportId");

            migrationBuilder.RenameColumn(
                name: "TotalBatches",
                schema: "pins",
                table: "ImportProcesses",
                newName: "TotalPlaces");

            migrationBuilder.RenameColumn(
                name: "BatchesProcessed",
                schema: "pins",
                table: "ImportProcesses",
                newName: "PlacesProcessed");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportStarredPlaces_Imports_ImportId",
                schema: "pins",
                table: "ImportStarredPlaces",
                column: "ImportId",
                principalSchema: "pins",
                principalTable: "Imports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE VIEW pins."ImportSummaries" AS
                SELECT
                    i."Id",
                    i."UserId",
                    i."ArchiveJobId",
                    i."State",
                    i."StartedAt",
                    i."CompletedAt",
                    i."PeriodStart",
                    i."PeriodEnd",
                    COALESCE(COUNT(sp."Id"), 0) AS "Total",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."IsProcessed" = true), 0) AS "Processed",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'New' AND sp."IsProcessed" = true), 0) AS "Created",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'Exists' AND sp."IsProcessed" = true), 0) AS "Updated",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'Invalid' AND sp."IsProcessed" = true), 0) AS "Failed",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'Conflicted' AND sp."IsProcessed" = true), 0) AS "Conflicts"
                FROM pins."Imports" i
                LEFT JOIN pins."ImportStarredPlaces" sp ON sp."ImportId" = i."Id"
                GROUP BY i."Id", i."UserId", i."ArchiveJobId", i."State",
                         i."StartedAt", i."CompletedAt", i."PeriodStart", i."PeriodEnd";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportStarredPlaces_Imports_ImportId",
                schema: "pins",
                table: "ImportStarredPlaces");

            migrationBuilder.RenameColumn(
                name: "ImportId",
                schema: "pins",
                table: "ImportStarredPlaces",
                newName: "BatchId");

            migrationBuilder.RenameIndex(
                name: "IX_ImportStarredPlaces_ImportId",
                schema: "pins",
                table: "ImportStarredPlaces",
                newName: "IX_ImportStarredPlaces_BatchId");

            migrationBuilder.RenameColumn(
                name: "TotalPlaces",
                schema: "pins",
                table: "ImportProcesses",
                newName: "TotalBatches");

            migrationBuilder.RenameColumn(
                name: "PlacesProcessed",
                schema: "pins",
                table: "ImportProcesses",
                newName: "BatchesProcessed");

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

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ImportId",
                schema: "pins",
                table: "ImportBatches",
                column: "ImportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportStarredPlaces_ImportBatches_BatchId",
                schema: "pins",
                table: "ImportStarredPlaces",
                column: "BatchId",
                principalSchema: "pins",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE VIEW pins."ImportSummaries" AS
                SELECT
                    i."Id",
                    i."UserId",
                    i."ArchiveJobId",
                    i."State",
                    i."StartedAt",
                    i."CompletedAt",
                    i."PeriodStart",
                    i."PeriodEnd",
                    COALESCE(COUNT(sp."Id"), 0) AS "Total",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."IsProcessed" = true), 0) AS "Processed",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'New'), 0) AS "Created",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'Exists'), 0) AS "Updated",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'Invalid'), 0) AS "Failed",
                    COALESCE(COUNT(sp."Id") FILTER (WHERE sp."State" = 'Conflicted'), 0) AS "Conflicts"
                FROM pins."Imports" i
                LEFT JOIN pins."ImportBatches" b ON b."ImportId" = i."Id"
                LEFT JOIN pins."ImportStarredPlaces" sp ON sp."BatchId" = b."Id"
                GROUP BY i."Id", i."UserId", i."ArchiveJobId", i."State",
                         i."StartedAt", i."CompletedAt", i."PeriodStart", i."PeriodEnd";
                """);
        }
    }
}