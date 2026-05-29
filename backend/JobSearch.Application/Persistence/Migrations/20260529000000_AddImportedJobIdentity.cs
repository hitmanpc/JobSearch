using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSearch.Application.Persistence.Migrations;

/// <inheritdoc />
public partial class AddImportedJobIdentity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var isPostgres = ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

        if (isPostgres)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Jobs" ADD COLUMN "Source" text NULL;
                ALTER TABLE "Jobs" ADD COLUMN "ExternalId" text NULL;
                ALTER TABLE "Jobs" ADD COLUMN "LastSeenAt" timestamp with time zone NULL;
                """);
        }
        else
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Jobs" ADD COLUMN "Source" TEXT NULL;
                ALTER TABLE "Jobs" ADD COLUMN "ExternalId" TEXT NULL;
                ALTER TABLE "Jobs" ADD COLUMN "LastSeenAt" TEXT NULL;
                """);
        }

        migrationBuilder.CreateIndex(
            name: "IX_Jobs_Source_ExternalId",
            table: "Jobs",
            columns: new[] { "Source", "ExternalId" },
            unique: true,
            filter: "\"Source\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Jobs_Source_Url",
            table: "Jobs",
            columns: new[] { "Source", "Url" },
            unique: true,
            filter: "\"Source\" IS NOT NULL AND \"ExternalId\" IS NULL AND \"Url\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Jobs_Source_ExternalId",
            table: "Jobs");

        migrationBuilder.DropIndex(
            name: "IX_Jobs_Source_Url",
            table: "Jobs");

        migrationBuilder.DropColumn(
            name: "ExternalId",
            table: "Jobs");

        migrationBuilder.DropColumn(
            name: "LastSeenAt",
            table: "Jobs");

        migrationBuilder.DropColumn(
            name: "Source",
            table: "Jobs");
    }
}
