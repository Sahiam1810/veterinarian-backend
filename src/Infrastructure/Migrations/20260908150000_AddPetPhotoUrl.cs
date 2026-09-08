using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

// Añade PHOTO_URL a PETS para persistir la foto por URL absoluta.
public partial class AddPetPhotoUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PHOTO_URL",
            table: "PETS",
            type: "NVARCHAR2(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PHOTO_URL",
            table: "PETS");
    }
}
