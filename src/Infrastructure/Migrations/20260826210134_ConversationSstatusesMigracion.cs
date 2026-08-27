using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConversationSstatusesMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONVERSATIONS_STATUSES",
                columns: table => new
                {
                    CONVERSATIONS_STATUSES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME_STATUS = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONVERSATIONS_STATUSES", x => x.CONVERSATIONS_STATUSES_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONVERSATIONS_STATUSES");
        }
    }
}
