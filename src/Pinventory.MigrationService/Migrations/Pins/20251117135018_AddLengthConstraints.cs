#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Pinventory.MigrationService.Migrations.Pins
{
    /// <inheritdoc />
    public partial class AddLengthConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the ImportSummaries view before altering columns
            migrationBuilder.Sql("""DROP VIEW IF EXISTS pins."ImportSummaries";""");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                schema: "pins",
                table: "TagCatalogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                schema: "pins",
                table: "PinTags",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "pins",
                table: "Pins",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PlaceId",
                schema: "pins",
                table: "Pins",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                schema: "pins",
                table: "Pins",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "pins",
                table: "Pins",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "pins",
                table: "Pins",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "pins",
                table: "Pins",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GoogleMapsUrl",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "pins",
                table: "Imports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                schema: "pins",
                table: "Imports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ArchiveJobId",
                schema: "pins",
                table: "Imports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                schema: "pins",
                table: "CatalogTags",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // Recreate the ImportSummaries view after altering columns
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
            // Drop the ImportSummaries view before reverting column changes
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS pins.""ImportSummaries"";");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                schema: "pins",
                table: "TagCatalogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                schema: "pins",
                table: "PinTags",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PlaceId",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "pins",
                table: "Pins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GoogleMapsUrl",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "pins",
                table: "ImportStarredPlaces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "pins",
                table: "Imports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                schema: "pins",
                table: "Imports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ArchiveJobId",
                schema: "pins",
                table: "Imports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                schema: "pins",
                table: "CatalogTags",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // Recreate the ImportSummaries view after reverting column changes
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
    }
}