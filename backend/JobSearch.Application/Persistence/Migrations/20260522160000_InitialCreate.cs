using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSearch.Application.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260522160000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var isPostgres = ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

        if (isPostgres)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "Jobs" (
                    "Id" uuid NOT NULL,
                    "Company" text NOT NULL,
                    "Title" text NOT NULL,
                    "Location" text NULL,
                    "RemoteType" integer NOT NULL,
                    "Url" text NULL,
                    "Description" text NULL,
                    "Status" integer NOT NULL,
                    "FitScore" integer NULL,
                    "GeneratedRecruiterMessage" text NULL,
                    "DateFound" timestamp with time zone NOT NULL,
                    "DateApplied" timestamp with time zone NULL,
                    "FitScoreResult_FitScore" integer NULL,
                    "FitScoreResult_MatchingSkills" text NULL,
                    "FitScoreResult_MissingSkills" text NULL,
                    "FitScoreResult_Concerns" text NULL,
                    "FitScoreResult_RecommendedAction" text NULL,
                    CONSTRAINT "PK_Jobs" PRIMARY KEY ("Id")
                );
                """);

            return;
        }

        migrationBuilder.Sql("""
            CREATE TABLE "Jobs" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Jobs" PRIMARY KEY,
                "Company" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "Location" TEXT NULL,
                "RemoteType" INTEGER NOT NULL,
                "Url" TEXT NULL,
                "Description" TEXT NULL,
                "Status" INTEGER NOT NULL,
                "FitScore" INTEGER NULL,
                "GeneratedRecruiterMessage" TEXT NULL,
                "DateFound" TEXT NOT NULL,
                "DateApplied" TEXT NULL,
                "FitScoreResult_FitScore" INTEGER NULL,
                "FitScoreResult_MatchingSkills" TEXT NULL,
                "FitScoreResult_MissingSkills" TEXT NULL,
                "FitScoreResult_Concerns" TEXT NULL,
                "FitScoreResult_RecommendedAction" TEXT NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Jobs");
    }
}
