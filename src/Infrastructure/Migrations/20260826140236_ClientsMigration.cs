using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClientsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CREATED_AT",
                table: "SPECIES",
                type: "TIMESTAMP(7)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SPECIES",
                type: "TIMESTAMP(7)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CREATED_AT",
                table: "RACES",
                type: "TIMESTAMP(7)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UPDATED_AT",
                table: "RACES",
                type: "TIMESTAMP(7)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CLIENTS",
                columns: table => new
                {
                    CLIENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    IDENTIFICATION_NUMBER = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    ADDRESS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    REGISTRATION_DATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIENTS", x => x.CLIENT_ID);
                    table.ForeignKey(
                        name: "FK_CLIENTS_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_IDENTIFICATION_NUMBER",
                table: "CLIENTS",
                column: "IDENTIFICATION_NUMBER",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_USER_ID",
                table: "CLIENTS",
                column: "USER_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CLIENTS");

            migrationBuilder.DropColumn(
                name: "CREATED_AT",
                table: "SPECIES");

            migrationBuilder.DropColumn(
                name: "UPDATED_AT",
                table: "SPECIES");

            migrationBuilder.DropColumn(
                name: "CREATED_AT",
                table: "RACES");

            migrationBuilder.DropColumn(
                name: "UPDATED_AT",
                table: "RACES");
        }
    }
}
