using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WidenClientAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ADDRESS",
                table: "CLIENTS",
                type: "NVARCHAR2(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ADDRESS",
                table: "CLIENTS",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
