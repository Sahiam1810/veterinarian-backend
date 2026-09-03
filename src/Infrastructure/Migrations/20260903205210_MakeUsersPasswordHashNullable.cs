using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeUsersPasswordHashNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PASSWORD_HASH",
                table: "USERS",
                type: "VARCHAR2(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(255)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PASSWORD_HASH",
                table: "USERS",
                type: "VARCHAR2(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR2(255)",
                oldNullable: true);
        }
    }
}
