using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatAiRunsAndMetricsAndErrorMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CHAT_AI_RUNS",
                columns: table => new
                {
                    CHAT_AI_RUNS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_MESSAGES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AI_MODEL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AI_RUNS_STATUSES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_AI_RUNS", x => x.CHAT_AI_RUNS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_AI_MODELS_AI_MODEL_ID",
                        column: x => x.AI_MODEL_ID,
                        principalTable: "AI_MODELS",
                        principalColumn: "AI_MODEL_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_AI_RUNS_STATUSES_AI_RUNS_STATUSES_ID",
                        column: x => x.AI_RUNS_STATUSES_ID,
                        principalTable: "AI_RUNS_STATUSES",
                        principalColumn: "AI_RUNS_STATUSES_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID",
                        column: x => x.CHAT_CONVERSATIONS_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_CHAT_MESSAGES_CHAT_MESSAGES_ID",
                        column: x => x.CHAT_MESSAGES_ID,
                        principalTable: "CHAT_MESSAGES",
                        principalColumn: "CHAT_MESSAGES_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_AI_RUN_ERRORS",
                columns: table => new
                {
                    CHAT_AI_RUN_ERRORS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_AI_RUNS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ERROR_MESSAGE = table.Column<string>(type: "CLOB", nullable: false),
                    ERROR_CODE = table.Column<string>(type: "VARCHAR2(80)", maxLength: 80, nullable: true),
                    PROVIDER_ERROR_ID = table.Column<string>(type: "VARCHAR2(120)", maxLength: 120, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_AI_RUN_ERRORS", x => x.CHAT_AI_RUN_ERRORS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUN_ERRORS_CHAT_AI_RUNS_CHAT_AI_RUNS_ID",
                        column: x => x.CHAT_AI_RUNS_ID,
                        principalTable: "CHAT_AI_RUNS",
                        principalColumn: "CHAT_AI_RUNS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_AI_RUN_METRICS",
                columns: table => new
                {
                    CHAT_AI_RUN_METRICS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_AI_RUNS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PROMPT_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0),
                    COMPLETION_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0),
                    TOTAL_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0),
                    COST = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false, defaultValue: 0m),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_AI_RUN_METRICS", x => x.CHAT_AI_RUN_METRICS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUN_METRICS_CHAT_AI_RUNS_CHAT_AI_RUNS_ID",
                        column: x => x.CHAT_AI_RUNS_ID,
                        principalTable: "CHAT_AI_RUNS",
                        principalColumn: "CHAT_AI_RUNS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUN_ERRORS_CHAT_AI_RUNS_ID",
                table: "CHAT_AI_RUN_ERRORS",
                column: "CHAT_AI_RUNS_ID");

            migrationBuilder.CreateIndex(
                name: "UX_CHAT_AI_RUN_METRICS_CHAT_AI_RUNS_ID",
                table: "CHAT_AI_RUN_METRICS",
                column: "CHAT_AI_RUNS_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_AI_MODELS_ID",
                table: "CHAT_AI_RUNS",
                column: "AI_MODEL_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_AI_RUN_STATUSES_ID",
                table: "CHAT_AI_RUNS",
                column: "AI_RUNS_STATUSES_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_CHAT_CONVERSATIONS_ID",
                table: "CHAT_AI_RUNS",
                column: "CHAT_CONVERSATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_CHAT_CONVERSATIONS_ID_CREATED_AT",
                table: "CHAT_AI_RUNS",
                columns: new[] { "CHAT_CONVERSATIONS_ID", "CREATED_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_CHAT_MESSAGES_ID",
                table: "CHAT_AI_RUNS",
                column: "CHAT_MESSAGES_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHAT_AI_RUN_ERRORS");

            migrationBuilder.DropTable(
                name: "CHAT_AI_RUN_METRICS");

            migrationBuilder.DropTable(
                name: "CHAT_AI_RUNS");
        }
    }
}
