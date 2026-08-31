using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TelegramActiveUserLinkIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TELEGRAM_USER_LINKS_CHAT",
                table: "TELEGRAM_USER_LINKS");

            migrationBuilder.DropIndex(
                name: "UX_TELEGRAM_USER_LINKS_PERSON",
                table: "TELEGRAM_USER_LINKS");

            migrationBuilder.DropIndex(
                name: "UX_TELEGRAM_USER_LINKS_USER",
                table: "TELEGRAM_USER_LINKS");

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_USER_LINKS_PERSON_ID",
                table: "TELEGRAM_USER_LINKS",
                column: "PERSON_ID");

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_TELEGRAM_USER_LINKS_PERSON"
                ON "TELEGRAM_USER_LINKS"
                (CASE WHEN "UNLINKED_AT" IS NULL THEN "PERSON_ID" END)
                """);
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_TELEGRAM_USER_LINKS_USER"
                ON "TELEGRAM_USER_LINKS"
                (CASE WHEN "UNLINKED_AT" IS NULL THEN "TELEGRAM_USER_ID" END)
                """);
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_TELEGRAM_USER_LINKS_CHAT"
                ON "TELEGRAM_USER_LINKS"
                (CASE WHEN "UNLINKED_AT" IS NULL THEN "TELEGRAM_CHAT_ID" END)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"UX_TELEGRAM_USER_LINKS_CHAT\"");
            migrationBuilder.Sql("DROP INDEX \"UX_TELEGRAM_USER_LINKS_USER\"");
            migrationBuilder.Sql("DROP INDEX \"UX_TELEGRAM_USER_LINKS_PERSON\"");

            migrationBuilder.DropIndex(
                name: "IX_TELEGRAM_USER_LINKS_PERSON_ID",
                table: "TELEGRAM_USER_LINKS");

            migrationBuilder.CreateIndex(
                name: "UX_TELEGRAM_USER_LINKS_CHAT",
                table: "TELEGRAM_USER_LINKS",
                column: "TELEGRAM_CHAT_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TELEGRAM_USER_LINKS_PERSON",
                table: "TELEGRAM_USER_LINKS",
                column: "PERSON_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TELEGRAM_USER_LINKS_USER",
                table: "TELEGRAM_USER_LINKS",
                column: "TELEGRAM_USER_ID",
                unique: true);
        }
    }
}
