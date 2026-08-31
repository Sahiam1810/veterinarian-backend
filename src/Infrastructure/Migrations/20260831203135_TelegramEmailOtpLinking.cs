using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TelegramEmailOtpLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TELEGRAM_LINKING_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    TELEGRAM_USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_CHAT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    EMAIL_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_LINKING_SESSIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TELEGRAM_LINKING_SESSIONS_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINKING_SESSIONS_ACTIVE",
                table: "TELEGRAM_LINKING_SESSIONS",
                columns: new[] { "TELEGRAM_USER_ID", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINKING_SESSIONS_EMAIL",
                table: "TELEGRAM_LINKING_SESSIONS",
                column: "EMAIL_HASH");

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINKING_SESSIONS_PERSON_ID",
                table: "TELEGRAM_LINKING_SESSIONS",
                column: "PERSON_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TELEGRAM_LINKING_SESSIONS");
        }
    }
}
