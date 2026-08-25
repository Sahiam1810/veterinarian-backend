using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRolesCodeSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "11111111-1111-1111-1111-111111111111");

            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "22222222-2222-2222-2222-222222222222");

            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "33333333-3333-3333-3333-333333333333");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "ROLE_ID", "CREATED_AT", "DESCRIPTION", "NAME" },
                values: new object[,]
                {
                    { "11111111-1111-1111-1111-111111111111", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", "Administrator" },
                    { "22222222-2222-2222-2222-222222222222", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Help desk support agent", "Agent" },
                    { "33333333-3333-3333-3333-333333333333", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Help desk client", "Client" }
                });
        }
    }
}
