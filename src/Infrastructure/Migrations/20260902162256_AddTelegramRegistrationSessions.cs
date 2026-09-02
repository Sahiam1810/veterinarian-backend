using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramRegistrationSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TELEGRAM_REGISTRATION_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    TELEGRAM_USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_CHAT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    PROTECTED_EMAIL = table.Column<string>(type: "VARCHAR2(2048)", maxLength: 2048, nullable: true),
                    EMAIL_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    COMPLETION_TOKEN_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    ACCOUNT_KIND = table.Column<string>(type: "VARCHAR2(24)", maxLength: 24, nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(24)", maxLength: 24, nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    OTP_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    COMPLETION_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_REGISTRATION_SESSIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TG_REG_SESSIONS_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_REGISTRATION_SESSIONS_PERSON_ID",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                column: "PERSON_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TG_REG_SESSIONS_ACTIVE",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                columns: new[] { "TELEGRAM_USER_ID", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_TG_REG_SESSIONS_EMAIL",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                column: "EMAIL_HASH");

            migrationBuilder.CreateIndex(
                name: "UX_TG_REG_SESSIONS_TOKEN",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                column: "COMPLETION_TOKEN_HASH",
                unique: true,
                filter: "\"COMPLETION_TOKEN_HASH\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TELEGRAM_REGISTRATION_SESSIONS");
        }
    }
}
