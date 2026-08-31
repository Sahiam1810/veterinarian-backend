using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatMessagesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CHAT_MESSAGES",
                columns: table => new
                {
                    CHAT_MESSAGES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    SENDER_TYPES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    MESSAGE_TYPE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_PARTICIPANTS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CONTENT = table.Column<string>(type: "CLOB", nullable: false),
                    METADATA = table.Column<string>(type: "CLOB", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_MESSAGES", x => x.CHAT_MESSAGES_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_MESSAGES_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID",
                        column: x => x.CHAT_CONVERSATIONS_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_MESSAGES_CHAT_PARTICIPANTS_CHAT_PARTICIPANTS_ID",
                        column: x => x.CHAT_PARTICIPANTS_ID,
                        principalTable: "CHAT_PARTICIPANTS",
                        principalColumn: "CHAT_PARTICIPANTS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_MESSAGES_MESSAGE_TYPES_MESSAGE_TYPE_ID",
                        column: x => x.MESSAGE_TYPE_ID,
                        principalTable: "MESSAGE_TYPES",
                        principalColumn: "MESSAGE_TYPES_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_MESSAGES_SENDER_TYPES_SENDER_TYPES_ID",
                        column: x => x.SENDER_TYPES_ID,
                        principalTable: "SENDER_TYPES",
                        principalColumn: "SENDER_TYPES_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_ATTACHMENTS",
                columns: table => new
                {
                    CHAT_ATTACHMENTS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_MESSAGES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    FILE_URL = table.Column<string>(type: "VARCHAR2(1000)", maxLength: 1000, nullable: false),
                    FILE_TYPE = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: false),
                    FILE_NAME = table.Column<string>(type: "VARCHAR2(255)", maxLength: 255, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ATTACHMENTS", x => x.CHAT_ATTACHMENTS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ATTACHMENTS_CHAT_MESSAGES_CHAT_MESSAGES_ID",
                        column: x => x.CHAT_MESSAGES_ID,
                        principalTable: "CHAT_MESSAGES",
                        principalColumn: "CHAT_MESSAGES_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ATTACHMENTS_CHAT_MESSAGES_ID",
                table: "CHAT_ATTACHMENTS",
                column: "CHAT_MESSAGES_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_MESSAGES_CHAT_CONVERSATIONS_ID",
                table: "CHAT_MESSAGES",
                column: "CHAT_CONVERSATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_MESSAGES_CHAT_PARTICIPANTS_ID",
                table: "CHAT_MESSAGES",
                column: "CHAT_PARTICIPANTS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_MESSAGES_MESSAGE_TYPE_ID",
                table: "CHAT_MESSAGES",
                column: "MESSAGE_TYPE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_MESSAGES_SENDER_TYPES_ID",
                table: "CHAT_MESSAGES",
                column: "SENDER_TYPES_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHAT_ATTACHMENTS");

            migrationBuilder.DropTable(
                name: "CHAT_MESSAGES");
        }
    }
}
