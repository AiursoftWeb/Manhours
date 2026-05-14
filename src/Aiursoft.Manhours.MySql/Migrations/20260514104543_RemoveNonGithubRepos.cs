using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.Manhours.MySql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNonGithubRepos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove RepoContributions for non-github.com repos first (FK constraint)
            migrationBuilder.Sql(
                "DELETE FROM `RepoContributions` " +
                "WHERE `RepoId` IN (" +
                "  SELECT `Id` FROM `Repos` " +
                "  WHERE `Url` NOT LIKE 'https://github.com/%' " +
                "  AND `Url` NOT LIKE 'http://github.com/%'" +
                ");");

            // Then remove the repos themselves
            migrationBuilder.Sql(
                "DELETE FROM `Repos` " +
                "WHERE `Url` NOT LIKE 'https://github.com/%' " +
                "AND `Url` NOT LIKE 'http://github.com/%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot restore deleted repos — one-way cleanup
        }
    }
}
