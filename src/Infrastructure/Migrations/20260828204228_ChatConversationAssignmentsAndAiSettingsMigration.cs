using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatConversationAssignmentsAndAiSettingsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CHAT_CONVERSATION_AI_SETTINGS",
                columns: table => new
                {
                    CHAT_CONVERSATION_AI_SETTING_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CONVERSATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AI_ENABLED = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    DEFAULT_MODEL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_CONVERSATION_AI_SETTINGS", x => x.CHAT_CONVERSATION_AI_SETTING_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_AI_SETTINGS_AI_MODELS_DEFAULT_MODEL_ID",
                        column: x => x.DEFAULT_MODEL_ID,
                        principalTable: "AI_MODELS",
                        principalColumn: "AI_MODEL_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_AI_SETTINGS_CHAT_CONVERSATIONS_CONVERSATION_ID",
                        column: x => x.CONVERSATION_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_CONVERSATION_ASSIGNMENTS",
                columns: table => new
                {
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AGENT_HUMAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    ASSIGNED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    UNASSIGNED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_CONVERSATION_ASSIGNMENTS", x => x.CHAT_CONVERSATIONS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_ASSIGNMENTS_AGENT_HUMANS_AGENT_HUMAN_ID",
                        column: x => x.AGENT_HUMAN_ID,
                        principalTable: "AGENT_HUMANS",
                        principalColumn: "AGENT_HUMAN_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_ASSIGNMENTS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID",
                        column: x => x.CHAT_CONVERSATIONS_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATION_AI_SETTINGS_CONVERSATION_ID",
                table: "CHAT_CONVERSATION_AI_SETTINGS",
                column: "CONVERSATION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATION_AI_SETTINGS_DEFAULT_MODEL_ID",
                table: "CHAT_CONVERSATION_AI_SETTINGS",
                column: "DEFAULT_MODEL_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATION_ASSIGNMENTS_AGENT_HUMAN_ID",
                table: "CHAT_CONVERSATION_ASSIGNMENTS",
                column: "AGENT_HUMAN_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHAT_CONVERSATION_AI_SETTINGS");

            migrationBuilder.DropTable(
                name: "CHAT_CONVERSATION_ASSIGNMENTS");
        }
    }
}
