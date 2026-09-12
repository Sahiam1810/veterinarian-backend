using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramIdentitySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TELEGRAM_IDENTITY_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    TELEGRAM_USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_CHAT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    PROTECTED_IDENTIFICATION = table.Column<string>(type: "VARCHAR2(512)", maxLength: 512, nullable: true),
                    PROTECTED_FULL_NAME = table.Column<string>(type: "VARCHAR2(1024)", maxLength: 1024, nullable: true),
                    PROTECTED_EMAIL = table.Column<string>(type: "VARCHAR2(1024)", maxLength: 1024, nullable: true),
                    PROTECTED_PENDING_MESSAGE = table.Column<string>(type: "CLOB", nullable: true),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(40)", maxLength: 40, nullable: false),
                    OTP_ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    OTP_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    ABSOLUTE_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    IDLE_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    PENDING_INBOUND_UPDATE_ID = table.Column<long>(type: "NUMBER(19)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_IDENTITY_SESSIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TG_ID_SESSIONS_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_IDENTITY_SESSIONS_PERSON_ID",
                table: "TELEGRAM_IDENTITY_SESSIONS",
                column: "PERSON_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TG_ID_SESSIONS_CHAT",
                table: "TELEGRAM_IDENTITY_SESSIONS",
                column: "TELEGRAM_CHAT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TG_ID_SESSIONS_USER_STATUS",
                table: "TELEGRAM_IDENTITY_SESSIONS",
                columns: new[] { "TELEGRAM_USER_ID", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "UX_TG_ID_SESSIONS_PENDING",
                table: "TELEGRAM_IDENTITY_SESSIONS",
                column: "PENDING_INBOUND_UPDATE_ID",
                unique: true,
                filter: "\"PENDING_INBOUND_UPDATE_ID\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TELEGRAM_IDENTITY_SESSIONS");
        }
    }
}
