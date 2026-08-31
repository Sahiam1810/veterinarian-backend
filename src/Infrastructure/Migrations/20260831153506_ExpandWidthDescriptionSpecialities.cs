using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandWidthDescriptionSpecialities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NAME",
                table: "SPECIALTIES",
                type: "VARCHAR2(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "DESCRIPTION",
                table: "SPECIALTIES",
                type: "VARCHAR2(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NAME",
                table: "SPECIALTIES",
                type: "VARCHAR2(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "DESCRIPTION",
                table: "SPECIALTIES",
                type: "VARCHAR2(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(120)",
                oldMaxLength: 120,
                oldNullable: true);
        }
    }
}
