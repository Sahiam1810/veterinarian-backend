using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientsPhoneNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_CLIENTS_PHONE_NUMBER",
                table: "CLIENTS",
                column: "PHONE_NUMBER",
                unique: true,
                filter: "\"PHONE_NUMBER\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CLIENTS_PHONE_NUMBER",
                table: "CLIENTS");
        }
    }
}
