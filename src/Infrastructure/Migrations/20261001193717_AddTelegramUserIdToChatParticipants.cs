using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramUserIdToChatParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CHAT_PARTICIPANTS_ONE_IDENTITY",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.AddColumn<long>(
                name: "TELEGRAM_USER_ID",
                table: "CHAT_PARTICIPANTS",
                type: "NUMBER(19)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_PARTICIPANTS_TELEGRAM_USER_ID",
                table: "CHAT_PARTICIPANTS",
                column: "TELEGRAM_USER_ID");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CHAT_PARTICIPANTS_ONE_IDENTITY",
                table: "CHAT_PARTICIPANTS",
                sql: "(\"CLIENT_ID\" IS NOT NULL AND \"AGENT_HUMAN_ID\" IS NULL AND \"TELEGRAM_USER_ID\" IS NULL) OR (\"CLIENT_ID\" IS NULL AND \"AGENT_HUMAN_ID\" IS NOT NULL AND \"TELEGRAM_USER_ID\" IS NULL) OR (\"CLIENT_ID\" IS NULL AND \"AGENT_HUMAN_ID\" IS NULL AND \"TELEGRAM_USER_ID\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CHAT_PARTICIPANTS_TELEGRAM_USER_ID",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CHAT_PARTICIPANTS_ONE_IDENTITY",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.DropColumn(
                name: "TELEGRAM_USER_ID",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CHAT_PARTICIPANTS_ONE_IDENTITY",
                table: "CHAT_PARTICIPANTS",
                sql: "(\"CLIENT_ID\" IS NOT NULL AND \"AGENT_HUMAN_ID\" IS NULL) OR (\"CLIENT_ID\" IS NULL AND \"AGENT_HUMAN_ID\" IS NOT NULL)");
        }
    }
}
