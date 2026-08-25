using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RolesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ROLES",
                columns: table => new
                {
                    ROLE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "CLOB", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLES", x => x.ROLE_ID);
                });

            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "ROLE_ID", "CREATED_AT", "DESCRIPTION", "NAME" },
                values: new object[,]
                {
                    { "11111111-1111-1111-1111-111111111111", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", "Administrator" },
                    { "22222222-2222-2222-2222-222222222222", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Help desk support agent", "Agent" },
                    { "33333333-3333-3333-3333-333333333333", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Help desk client", "Client" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ROLES_NAME",
                table: "ROLES",
                column: "NAME",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ROLES");
        }
    }
}
