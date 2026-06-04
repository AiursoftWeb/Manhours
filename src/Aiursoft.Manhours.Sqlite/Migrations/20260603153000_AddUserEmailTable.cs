using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.Manhours.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEmails_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmails_UserId",
                table: "UserEmails",
                column: "UserId");

            migrationBuilder.Sql("""
                INSERT INTO "UserEmails" ("Email", "CreatedTime", "UserId")
                SELECT "Email", CURRENT_TIMESTAMP, "Id"
                FROM "AspNetUsers"
                WHERE "Email" IS NOT NULL AND "Email" <> ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmails");
        }
    }
}
