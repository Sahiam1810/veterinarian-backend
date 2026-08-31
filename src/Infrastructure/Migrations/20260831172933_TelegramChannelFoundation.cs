using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TelegramChannelFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TELEGRAM_INBOUND_UPDATES",
                columns: table => new
                {
                    UPDATE_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_CHAT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_MESSAGE_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    CHAT_TYPE = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    MESSAGE_TEXT = table.Column<string>(type: "CLOB", nullable: true),
                    RESPONSE_TEXT = table.Column<string>(type: "CLOB", nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NEXT_ATTEMPT_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LAST_SENT_CHUNK_INDEX = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    LAST_ERROR_CODE = table.Column<string>(type: "VARCHAR2(120)", maxLength: 120, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_INBOUND_UPDATES", x => x.UPDATE_ID);
                });

            migrationBuilder.CreateTable(
                name: "TELEGRAM_LINK_CODES",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CODE_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    CONSUMED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    INVALIDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_LINK_CODES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TELEGRAM_LINK_CODES_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TELEGRAM_USER_LINKS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    TELEGRAM_USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_CHAT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    LINKED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_USER_LINKS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TELEGRAM_USER_LINKS_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TELEGRAM_CONVERSATION_LINKS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    TELEGRAM_USER_LINK_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CONVERSATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_CONVERSATION_LINKS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TELEGRAM_CONVERSATION_LINKS_CONVERSATION",
                        column: x => x.CONVERSATION_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TELEGRAM_CONVERSATION_LINKS_USER_LINK",
                        column: x => x.TELEGRAM_USER_LINK_ID,
                        principalTable: "TELEGRAM_USER_LINKS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_TELEGRAM_CONVERSATION_LINKS_CONVERSATION",
                table: "TELEGRAM_CONVERSATION_LINKS",
                column: "CONVERSATION_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TELEGRAM_CONVERSATION_LINKS_USER",
                table: "TELEGRAM_CONVERSATION_LINKS",
                column: "TELEGRAM_USER_LINK_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_INBOUND_UPDATES_PENDING",
                table: "TELEGRAM_INBOUND_UPDATES",
                columns: new[] { "STATUS", "NEXT_ATTEMPT_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINK_CODES_PERSON",
                table: "TELEGRAM_LINK_CODES",
                column: "PERSON_ID");

            migrationBuilder.CreateIndex(
                name: "UX_TELEGRAM_LINK_CODES_HASH",
                table: "TELEGRAM_LINK_CODES",
                column: "CODE_HASH",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TELEGRAM_CONVERSATION_LINKS");

            migrationBuilder.DropTable(
                name: "TELEGRAM_INBOUND_UPDATES");

            migrationBuilder.DropTable(
                name: "TELEGRAM_LINK_CODES");

            migrationBuilder.DropTable(
                name: "TELEGRAM_USER_LINKS");
        }
    }
}
