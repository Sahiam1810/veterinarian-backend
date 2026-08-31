using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatParticipantsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CHAT_PARTICIPANTS",
                columns: table => new
                {
                    CHAT_PARTICIPANTS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PARTICIPANT_TYPE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_USER_PROFILE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    AGENT_HUMAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    AI_MODEL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_PARTICIPANTS", x => x.CHAT_PARTICIPANTS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_PARTICIPANTS_AGENT_HUMANS_AGENT_HUMAN_ID",
                        column: x => x.AGENT_HUMAN_ID,
                        principalTable: "AGENT_HUMANS",
                        principalColumn: "AGENT_HUMAN_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_PARTICIPANTS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID",
                        column: x => x.CHAT_CONVERSATIONS_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_PARTICIPANTS_CHAT_USER_PROFILES_CHAT_USER_PROFILE_ID",
                        column: x => x.CHAT_USER_PROFILE_ID,
                        principalTable: "CHAT_USER_PROFILES",
                        principalColumn: "CHAT_USER_PROFILE_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_PARTICIPANTS_SENDER_TYPES_PARTICIPANT_TYPE_ID",
                        column: x => x.PARTICIPANT_TYPE_ID,
                        principalTable: "SENDER_TYPES",
                        principalColumn: "SENDER_TYPES_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_PARTICIPANTS_AGENT_HUMAN_ID",
                table: "CHAT_PARTICIPANTS",
                column: "AGENT_HUMAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_PARTICIPANTS_CHAT_CONVERSATIONS_ID",
                table: "CHAT_PARTICIPANTS",
                column: "CHAT_CONVERSATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_PARTICIPANTS_CHAT_USER_PROFILE_ID",
                table: "CHAT_PARTICIPANTS",
                column: "CHAT_USER_PROFILE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_PARTICIPANTS_PARTICIPANT_TYPE_ID",
                table: "CHAT_PARTICIPANTS",
                column: "PARTICIPANT_TYPE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHAT_PARTICIPANTS");
        }
    }
}
