#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class AddImportSummariesView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIEW pins."ImportSummaries" AS
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS pins.""ImportSummaries"";");
        }
    }
}