using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateClientsFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CHAT_PARTICIPANTS_CHAT_USER_PROFILES_CHAT_USER_PROFILE_ID",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.DropForeignKey(
                name: "FK_CLIENTS_USERS_USER_ID",
                table: "CLIENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_TELEGRAM_USER_LINKS_USERS",
                table: "TELEGRAM_USER_LINKS");

            migrationBuilder.DropTable(
                name: "CHAT_USER_PROFILES");

            migrationBuilder.DropIndex(
                name: "IX_CLIENTS_USER_ID",
                table: "CLIENTS");

            migrationBuilder.DropColumn(
                name: "USER_ID",
                table: "CLIENTS");

            migrationBuilder.DropColumn(
                name: "AI_MODEL_ID",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.RenameColumn(
                name: "PERSON_ID",
                table: "TELEGRAM_USER_LINKS",
                newName: "CLIENT_ID");

            migrationBuilder.RenameIndex(
                name: "IX_TELEGRAM_USER_LINKS_PERSON_ID",
                table: "TELEGRAM_USER_LINKS",
                newName: "IX_TELEGRAM_USER_LINKS_CLIENT_ID");

            migrationBuilder.RenameColumn(
                name: "CHAT_USER_PROFILE_ID",
                table: "CHAT_PARTICIPANTS",
                newName: "CLIENT_ID");

            migrationBuilder.RenameIndex(
                name: "IX_CHAT_PARTICIPANTS_CHAT_USER_PROFILE_ID",
                table: "CHAT_PARTICIPANTS",
                newName: "IX_CHAT_PARTICIPANTS_CLIENT_ID");

            migrationBuilder.AlterColumn<string>(
                name: "USER_ID",
                table: "NOTIFICATIONS",
                type: "VARCHAR2(36)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)");

            migrationBuilder.AddColumn<string>(
                name: "CLIENT_ID",
                table: "NOTIFICATIONS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_CLIENT_ID",
                table: "NOTIFICATIONS",
                column: "CLIENT_ID");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NOTIFICATIONS_ONE_RECIPIENT",
                table: "NOTIFICATIONS",
                sql: "(\"USER_ID\" IS NOT NULL AND \"CLIENT_ID\" IS NULL) OR (\"USER_ID\" IS NULL AND \"CLIENT_ID\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CHAT_PARTICIPANTS_ONE_IDENTITY",
                table: "CHAT_PARTICIPANTS",
                sql: "(\"CLIENT_ID\" IS NOT NULL AND \"AGENT_HUMAN_ID\" IS NULL) OR (\"CLIENT_ID\" IS NULL AND \"AGENT_HUMAN_ID\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_CHAT_PARTICIPANTS_CLIENTS_CLIENT_ID",
                table: "CHAT_PARTICIPANTS",
                column: "CLIENT_ID",
                principalTable: "CLIENTS",
                principalColumn: "CLIENT_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATIONS_CLIENTS_CLIENT_ID",
                table: "NOTIFICATIONS",
                column: "CLIENT_ID",
                principalTable: "CLIENTS",
                principalColumn: "CLIENT_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TELEGRAM_USER_LINKS_CLIENTS",
                table: "TELEGRAM_USER_LINKS",
                column: "CLIENT_ID",
                principalTable: "CLIENTS",
                principalColumn: "CLIENT_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CHAT_PARTICIPANTS_CLIENTS_CLIENT_ID",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATIONS_CLIENTS_CLIENT_ID",
                table: "NOTIFICATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_TELEGRAM_USER_LINKS_CLIENTS",
                table: "TELEGRAM_USER_LINKS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATIONS_CLIENT_ID",
                table: "NOTIFICATIONS");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NOTIFICATIONS_ONE_RECIPIENT",
                table: "NOTIFICATIONS");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CHAT_PARTICIPANTS_ONE_IDENTITY",
                table: "CHAT_PARTICIPANTS");

            migrationBuilder.DropColumn(
                name: "CLIENT_ID",
                table: "NOTIFICATIONS");

            migrationBuilder.RenameColumn(
                name: "CLIENT_ID",
                table: "TELEGRAM_USER_LINKS",
                newName: "PERSON_ID");

            migrationBuilder.RenameIndex(
                name: "IX_TELEGRAM_USER_LINKS_CLIENT_ID",
                table: "TELEGRAM_USER_LINKS",
                newName: "IX_TELEGRAM_USER_LINKS_PERSON_ID");

            migrationBuilder.RenameColumn(
                name: "CLIENT_ID",
                table: "CHAT_PARTICIPANTS",
                newName: "CHAT_USER_PROFILE_ID");

            migrationBuilder.RenameIndex(
                name: "IX_CHAT_PARTICIPANTS_CLIENT_ID",
                table: "CHAT_PARTICIPANTS",
                newName: "IX_CHAT_PARTICIPANTS_CHAT_USER_PROFILE_ID");

            migrationBuilder.AlterColumn<string>(
                name: "USER_ID",
                table: "NOTIFICATIONS",
                type: "VARCHAR2(36)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "USER_ID",
                table: "CLIENTS",
                type: "VARCHAR2(36)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AI_MODEL_ID",
                table: "CHAT_PARTICIPANTS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CHAT_USER_PROFILES",
                columns: table => new
                {
                    CHAT_USER_PROFILE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AVATAR_URL = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    BIO = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    DISPLAY_NAME = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_USER_PROFILES", x => x.CHAT_USER_PROFILE_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_USER_PROFILES_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_USER_ID",
                table: "CLIENTS",
                column: "USER_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_USER_PROFILES_USER_ID",
                table: "CHAT_USER_PROFILES",
                column: "USER_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_CHAT_PARTICIPANTS_CHAT_USER_PROFILES_CHAT_USER_PROFILE_ID",
                table: "CHAT_PARTICIPANTS",
                column: "CHAT_USER_PROFILE_ID",
                principalTable: "CHAT_USER_PROFILES",
                principalColumn: "CHAT_USER_PROFILE_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CLIENTS_USERS_USER_ID",
                table: "CLIENTS",
                column: "USER_ID",
                principalTable: "USERS",
                principalColumn: "USER_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TELEGRAM_USER_LINKS_USERS",
                table: "TELEGRAM_USER_LINKS",
                column: "PERSON_ID",
                principalTable: "USERS",
                principalColumn: "USER_ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
