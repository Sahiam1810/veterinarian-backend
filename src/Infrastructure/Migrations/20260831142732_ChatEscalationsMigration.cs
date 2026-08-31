using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatEscalationsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CHAT_ESCALATIONS",
                columns: table => new
                {
                    CHAT_ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    FROM_AI = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    REASON = table.Column<string>(type: "CLOB", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<string>(type: "VARCHAR2(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ESCALATIONS", x => x.CHAT_ESCALATIONS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_CONV_ID",
                        column: x => x.CHAT_CONVERSATIONS_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_STAT_ID",
                        column: x => x.ESCALATIONS_ID,
                        principalTable: "ESCALATIONS_STATUSES",
                        principalColumn: "ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_ESCALATION_ASSIGNMENTS",
                columns: table => new
                {
                    CHAT_ESCALATION_ASSIGNMENTS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AGENT_HUMAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ASSIGNED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ESCALATION_ASSIGNMENTS", x => x.CHAT_ESCALATION_ASSIGNMENTS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_ASG_AGT",
                        column: x => x.AGENT_HUMAN_ID,
                        principalTable: "AGENT_HUMANS",
                        principalColumn: "AGENT_HUMAN_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_ASG_ESC",
                        column: x => x.CHAT_ESCALATIONS_ID,
                        principalTable: "CHAT_ESCALATIONS",
                        principalColumn: "CHAT_ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_ESCALATION_RESOLUTION",
                columns: table => new
                {
                    CHAT_ESCALATION_RESOLUTION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    RESOLVED_BY = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    RESOLUTION_NOTE = table.Column<string>(type: "CLOB", nullable: true),
                    RESOLVED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ESCALATION_RESOLUTION", x => x.CHAT_ESCALATION_RESOLUTION_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_RES_ESC",
                        column: x => x.CHAT_ESCALATIONS_ID,
                        principalTable: "CHAT_ESCALATIONS",
                        principalColumn: "CHAT_ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_ESCALATION_STATUS_HISTORY",
                columns: table => new
                {
                    CHAT_ESCALATION_STATUS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ESC_STAT_HIST", x => x.CHAT_ESCALATION_STATUS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_HIST_ESC",
                        column: x => x.CHAT_ESCALATIONS_ID,
                        principalTable: "CHAT_ESCALATIONS",
                        principalColumn: "CHAT_ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_HIST_STA",
                        column: x => x.ESCALATIONS_ID,
                        principalTable: "ESCALATIONS_STATUSES",
                        principalColumn: "ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_ASG_AGT",
                table: "CHAT_ESCALATION_ASSIGNMENTS",
                column: "AGENT_HUMAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_ASG_ESC",
                table: "CHAT_ESCALATION_ASSIGNMENTS",
                column: "CHAT_ESCALATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_RES_ESC",
                table: "CHAT_ESCALATION_RESOLUTION",
                column: "CHAT_ESCALATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_HIST_ESC",
                table: "CHAT_ESCALATION_STATUS_HISTORY",
                column: "CHAT_ESCALATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_HIST_STA",
                table: "CHAT_ESCALATION_STATUS_HISTORY",
                column: "ESCALATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_CONV_ID",
                table: "CHAT_ESCALATIONS",
                column: "CHAT_CONVERSATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_STAT_ID",
                table: "CHAT_ESCALATIONS",
                column: "ESCALATIONS_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHAT_ESCALATION_ASSIGNMENTS");

            migrationBuilder.DropTable(
                name: "CHAT_ESCALATION_RESOLUTION");

            migrationBuilder.DropTable(
                name: "CHAT_ESCALATION_STATUS_HISTORY");

            migrationBuilder.DropTable(
                name: "CHAT_ESCALATIONS");
        }
    }
}
