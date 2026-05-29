using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSearch.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateProfileRemotivePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RemotiveCategory",
                table: "CandidateProfile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemotiveLimit",
                table: "CandidateProfile",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemotiveSearchText",
                table: "CandidateProfile",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemotiveCategory",
                table: "CandidateProfile");

            migrationBuilder.DropColumn(
                name: "RemotiveLimit",
                table: "CandidateProfile");

            migrationBuilder.DropColumn(
                name: "RemotiveSearchText",
                table: "CandidateProfile");
        }
    }
}
