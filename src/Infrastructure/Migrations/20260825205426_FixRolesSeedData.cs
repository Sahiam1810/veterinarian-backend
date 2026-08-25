using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRolesSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "22222222-2222-2222-2222-222222222222");

            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "33333333-3333-3333-3333-333333333333");

            migrationBuilder.UpdateData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "11111111-1111-1111-1111-111111111111",
                columns: new[] { "DESCRIPTION", "NAME" },
                values: new object[] { "Administrador del sistema", "Administrador" });

            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "ROLE_ID", "CREATED_AT", "DESCRIPTION", "NAME" },
                values: new object[,]
                {
                    { "44444444-4444-4444-4444-444444444444", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Consulta su agenda, atiende citas y registra la historia clínica de la mascota", "Veterinario" },
                    { "55555555-5555-5555-5555-555555555555", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Registra dueños y mascotas, agenda, reprograma y cancela citas", "Recepcionista" },
                    { "66666666-6666-6666-6666-666666666666", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Apoya el registro y la preparación de la atención", "Auxiliar" },
                    { "77777777-7777-7777-7777-777777777777", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Portal para ver sus mascotas y sus citas", "Cliente" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "44444444-4444-4444-4444-444444444444");

            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "55555555-5555-5555-5555-555555555555");

            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "66666666-6666-6666-6666-666666666666");

            migrationBuilder.DeleteData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "77777777-7777-7777-7777-777777777777");

            migrationBuilder.UpdateData(
                table: "ROLES",
                keyColumn: "ROLE_ID",
                keyValue: "11111111-1111-1111-1111-111111111111",
                columns: new[] { "DESCRIPTION", "NAME" },
                values: new object[] { "System administrator", "Administrator" });

            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "ROLE_ID", "CREATED_AT", "DESCRIPTION", "NAME" },
                values: new object[,]
                {
                    { "22222222-2222-2222-2222-222222222222", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Help desk support agent", "Agent" },
                    { "33333333-3333-3333-3333-333333333333", new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Utc), "Help desk client", "Client" }
                });
        }
    }
}
