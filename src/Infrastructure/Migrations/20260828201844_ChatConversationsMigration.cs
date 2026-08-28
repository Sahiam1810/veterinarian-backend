using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatConversationsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CHAT_CONVERSATIONS",
                columns: table => new
                {
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CONVERSATION_STATUS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PRIORITY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    AI_ENABLED = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    LAST_MESSAGE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CLOSED = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0),
                    CLOSED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CLOSED_BY = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_CONVERSATIONS", x => x.CHAT_CONVERSATIONS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATIONS_CONVERSATIONS_STATUSES_CONVERSATION_STATUS_ID",
                        column: x => x.CONVERSATION_STATUS_ID,
                        principalTable: "CONVERSATIONS_STATUSES",
                        principalColumn: "CONVERSATIONS_STATUSES_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATIONS_PRIORITY_PRIORITY_ID",
                        column: x => x.PRIORITY_ID,
                        principalTable: "PRIORITY",
                        principalColumn: "PRIORITY_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATIONS_CONVERSATION_STATUS_ID",
                table: "CHAT_CONVERSATIONS",
                column: "CONVERSATION_STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATIONS_PRIORITY_ID",
                table: "CHAT_CONVERSATIONS",
                column: "PRIORITY_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHAT_CONVERSATIONS");
        }
    }
}
