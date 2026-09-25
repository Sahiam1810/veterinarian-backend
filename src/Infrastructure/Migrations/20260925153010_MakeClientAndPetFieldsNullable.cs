using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeClientAndPetFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CLIENTS_IDENTIFICATION_NUMBER",
                table: "CLIENTS");

            migrationBuilder.DropIndex(
                name: "UX_CLIENTS_EMAIL",
                table: "CLIENTS");

            migrationBuilder.AlterColumn<decimal>(
                name: "WEIGHT",
                table: "PETS",
                type: "NUMBER(6,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "NUMBER(6,3)");

            migrationBuilder.AlterColumn<string>(
                name: "RACE_ID",
                table: "PETS",
                type: "VARCHAR2(36)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)");

            migrationBuilder.AlterColumn<int>(
                name: "AGE",
                table: "PETS",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<string>(
                name: "IDENTIFICATION_NUMBER",
                table: "CLIENTS",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "EMAIL",
                table: "CLIENTS",
                type: "VARCHAR2(150)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(150)");

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_IDENTIFICATION_NUMBER",
                table: "CLIENTS",
                column: "IDENTIFICATION_NUMBER",
                unique: true,
                filter: "\"IDENTIFICATION_NUMBER\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_CLIENTS_EMAIL",
                table: "CLIENTS",
                column: "EMAIL",
                unique: true,
                filter: "\"EMAIL\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CLIENTS_IDENTIFICATION_NUMBER",
                table: "CLIENTS");

            migrationBuilder.DropIndex(
                name: "UX_CLIENTS_EMAIL",
                table: "CLIENTS");

            migrationBuilder.AlterColumn<decimal>(
                name: "WEIGHT",
                table: "PETS",
                type: "NUMBER(6,3)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "NUMBER(6,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RACE_ID",
                table: "PETS",
                type: "VARCHAR2(36)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AGE",
                table: "PETS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IDENTIFICATION_NUMBER",
                table: "CLIENTS",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EMAIL",
                table: "CLIENTS",
                type: "VARCHAR2(150)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR2(150)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_IDENTIFICATION_NUMBER",
                table: "CLIENTS",
                column: "IDENTIFICATION_NUMBER",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CLIENTS_EMAIL",
                table: "CLIENTS",
                column: "EMAIL",
                unique: true);
        }
    }
}
