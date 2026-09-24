using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationStayClientPetFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_HOSPITALIZATION_STAYS_CLIENT_PET",
                table: "HOSPITALIZATION_STAYS",
                column: "CLIENT_PET_ID",
                principalTable: "CLIENTS_PETS",
                principalColumn: "CLIENT_PET_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HOSPITALIZATION_STAYS_CLIENT_PET",
                table: "HOSPITALIZATION_STAYS");
        }
    }
}
