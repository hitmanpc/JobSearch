using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSearch.Application.Persistence.Migrations;

/// <inheritdoc />
public partial class AddScheduledJobRunStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var isPostgres = ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

        if (isPostgres)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "ScheduledJobRunStatuses" (
                    "Id" integer NOT NULL,
                    "LastRunStartedAt" timestamp with time zone NULL,
                    "LastRunCompletedAt" timestamp with time zone NULL,
                    "LastRunSucceeded" boolean NULL,
                    "LastResult" character varying(64) NOT NULL,
                    "LastErrorMessage" character varying(2048) NULL,
                    "NextExpectedRunAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_ScheduledJobRunStatuses" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_ScheduledJobRunStatuses_Singleton" CHECK ("Id" = 1)
                );
                """);

            return;
        }

        migrationBuilder.Sql("""
            CREATE TABLE "ScheduledJobRunStatuses" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ScheduledJobRunStatuses" PRIMARY KEY,
                "LastRunStartedAt" TEXT NULL,
                "LastRunCompletedAt" TEXT NULL,
                "LastRunSucceeded" INTEGER NULL,
                "LastResult" TEXT NOT NULL,
                "LastErrorMessage" TEXT NULL,
                "NextExpectedRunAt" TEXT NULL,
                CONSTRAINT "CK_ScheduledJobRunStatuses_Singleton" CHECK ("Id" = 1)
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ScheduledJobRunStatuses");
    }
}
