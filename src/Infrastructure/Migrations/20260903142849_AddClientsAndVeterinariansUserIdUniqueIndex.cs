using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientsAndVeterinariansUserIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VETERINARIANS_USER_ID",
                table: "VETERINARIANS");

            migrationBuilder.DropIndex(
                name: "IX_CLIENTS_USER_ID",
                table: "CLIENTS");

            migrationBuilder.CreateIndex(
                name: "IX_VETERINARIANS_USER_ID",
                table: "VETERINARIANS",
                column: "USER_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_USER_ID",
                table: "CLIENTS",
                column: "USER_ID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VETERINARIANS_USER_ID",
                table: "VETERINARIANS");

            migrationBuilder.DropIndex(
                name: "IX_CLIENTS_USER_ID",
                table: "CLIENTS");

            migrationBuilder.CreateIndex(
                name: "IX_VETERINARIANS_USER_ID",
                table: "VETERINARIANS",
                column: "USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_USER_ID",
                table: "CLIENTS",
                column: "USER_ID");
        }
    }
}
