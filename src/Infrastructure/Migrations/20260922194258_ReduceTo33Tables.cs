using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReduceTo33Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // U8: columnas nuevas ANTES que nada -- hacen falta para poder
            // hacer el backfill desde las tablas viejas antes de dropearlas.
            migrationBuilder.AddColumn<string>(
                name: "CHAT_CONVERSATION_ID",
                table: "TELEGRAM_USER_LINKS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RESOLUTION_NOTE",
                table: "CHAT_ESCALATIONS",
                type: "CLOB",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RESOLVED_AT",
                table: "CHAT_ESCALATIONS",
                type: "TIMESTAMP",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RESOLVED_BY",
                table: "CHAT_ESCALATIONS",
                type: "VARCHAR2(36)",
                nullable: true);

            // Backfill ANTES de los DropTable -- mismo patrón que MergeUsersAndAccounts (U5).
            migrationBuilder.Sql(@"
                UPDATE CHAT_ESCALATIONS e
                SET (RESOLVED_AT, RESOLVED_BY, RESOLUTION_NOTE) = (
                    SELECT r.RESOLVED_AT, r.RESOLVED_BY, r.RESOLUTION_NOTE
                    FROM CHAT_ESCALATION_RESOLUTION r
                    WHERE r.CHAT_ESCALATIONS_ID = e.CHAT_ESCALATIONS_ID
                )
                WHERE EXISTS (
                    SELECT 1 FROM CHAT_ESCALATION_RESOLUTION r
                    WHERE r.CHAT_ESCALATIONS_ID = e.CHAT_ESCALATIONS_ID
                )
            ");

            migrationBuilder.Sql(@"
                UPDATE TELEGRAM_USER_LINKS u
                SET CHAT_CONVERSATION_ID = (
                    SELECT l.CONVERSATION_ID
                    FROM TELEGRAM_CONVERSATION_LINKS l
                    WHERE l.TELEGRAM_USER_LINK_ID = u.ID
                )
                WHERE EXISTS (
                    SELECT 1 FROM TELEGRAM_CONVERSATION_LINKS l
                    WHERE l.TELEGRAM_USER_LINK_ID = u.ID
                )
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_CHAT_CONVERSATIONS_CONVERSATIONS_STATUSES_CONVERSATION_STATUS_ID",
                table: "CHAT_CONVERSATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_CHAT_CONVERSATIONS_PRIORITY_PRIORITY_ID",
                table: "CHAT_CONVERSATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_CHAT_MESSAGES_MESSAGE_TYPES_MESSAGE_TYPE_ID",
                table: "CHAT_MESSAGES");

            migrationBuilder.DropIndex(
                name: "IX_CHAT_MESSAGES_MESSAGE_TYPE_ID",
                table: "CHAT_MESSAGES");

            migrationBuilder.DropIndex(
                name: "IX_CHAT_CONVERSATIONS_CONVERSATION_STATUS_ID",
                table: "CHAT_CONVERSATIONS");

            migrationBuilder.DropIndex(
                name: "IX_CHAT_CONVERSATIONS_PRIORITY_ID",
                table: "CHAT_CONVERSATIONS");

            migrationBuilder.DropColumn(
                name: "MESSAGE_TYPE_ID",
                table: "CHAT_MESSAGES");

            migrationBuilder.DropColumn(
                name: "CONVERSATION_STATUS_ID",
                table: "CHAT_CONVERSATIONS");

            migrationBuilder.DropColumn(
                name: "PRIORITY_ID",
                table: "CHAT_CONVERSATIONS");

            // Ahora sí, con los datos ya copiados a su nuevo lugar.
            migrationBuilder.DropTable(
                name: "CHAT_ESCALATION_RESOLUTION");

            migrationBuilder.DropTable(
                name: "CONVERSATIONS_STATUSES");

            migrationBuilder.DropTable(
                name: "MESSAGE_TYPES");

            migrationBuilder.DropTable(
                name: "PRIORITY");

            migrationBuilder.DropTable(
                name: "TELEGRAM_CONVERSATION_LINKS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CHAT_CONVERSATION_ID",
                table: "TELEGRAM_USER_LINKS");

            migrationBuilder.DropColumn(
                name: "RESOLUTION_NOTE",
                table: "CHAT_ESCALATIONS");

            migrationBuilder.DropColumn(
                name: "RESOLVED_AT",
                table: "CHAT_ESCALATIONS");

            migrationBuilder.DropColumn(
                name: "RESOLVED_BY",
                table: "CHAT_ESCALATIONS");

            migrationBuilder.AddColumn<string>(
                name: "MESSAGE_TYPE_ID",
                table: "CHAT_MESSAGES",
                type: "VARCHAR2(36)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CONVERSATION_STATUS_ID",
                table: "CHAT_CONVERSATIONS",
                type: "VARCHAR2(36)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PRIORITY_ID",
                table: "CHAT_CONVERSATIONS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CHAT_ESCALATION_RESOLUTION",
                columns: table => new
                {
                    CHAT_ESCALATION_RESOLUTION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    RESOLUTION_NOTE = table.Column<string>(type: "CLOB", nullable: true),
                    RESOLVED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    RESOLVED_BY = table.Column<string>(type: "VARCHAR2(36)", nullable: true)
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
                name: "CONVERSATIONS_STATUSES",
                columns: table => new
                {
                    CONVERSATIONS_STATUSES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    NAME_STATUS = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONVERSATIONS_STATUSES", x => x.CONVERSATIONS_STATUSES_ID);
                });

            migrationBuilder.CreateTable(
                name: "MESSAGE_TYPES",
                columns: table => new
                {
                    MESSAGE_TYPES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    NAME_TYPE = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MESSAGE_TYPES", x => x.MESSAGE_TYPES_ID);
                });

            migrationBuilder.CreateTable(
                name: "PRIORITY",
                columns: table => new
                {
                    PRIORITY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    NAME_PRIORITY = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRIORITY", x => x.PRIORITY_ID);
                });

            migrationBuilder.CreateTable(
                name: "TELEGRAM_CONVERSATION_LINKS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CONVERSATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TELEGRAM_USER_LINK_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
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
                name: "IX_CHAT_MESSAGES_MESSAGE_TYPE_ID",
                table: "CHAT_MESSAGES",
                column: "MESSAGE_TYPE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATIONS_CONVERSATION_STATUS_ID",
                table: "CHAT_CONVERSATIONS",
                column: "CONVERSATION_STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATIONS_PRIORITY_ID",
                table: "CHAT_CONVERSATIONS",
                column: "PRIORITY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_RES_ESC",
                table: "CHAT_ESCALATION_RESOLUTION",
                column: "CHAT_ESCALATIONS_ID");

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

            migrationBuilder.AddForeignKey(
                name: "FK_CHAT_CONVERSATIONS_CONVERSATIONS_STATUSES_CONVERSATION_STATUS_ID",
                table: "CHAT_CONVERSATIONS",
                column: "CONVERSATION_STATUS_ID",
                principalTable: "CONVERSATIONS_STATUSES",
                principalColumn: "CONVERSATIONS_STATUSES_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CHAT_CONVERSATIONS_PRIORITY_PRIORITY_ID",
                table: "CHAT_CONVERSATIONS",
                column: "PRIORITY_ID",
                principalTable: "PRIORITY",
                principalColumn: "PRIORITY_ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CHAT_MESSAGES_MESSAGE_TYPES_MESSAGE_TYPE_ID",
                table: "CHAT_MESSAGES",
                column: "MESSAGE_TYPE_ID",
                principalTable: "MESSAGE_TYPES",
                principalColumn: "MESSAGE_TYPES_ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
