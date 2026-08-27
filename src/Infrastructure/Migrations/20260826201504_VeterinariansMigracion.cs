using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VeterinariansMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VETERINARIANS",
                columns: table => new
                {
                    VETERINARIAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    SPECIALTY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    LICENSE_NUMBER = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VETERINARIANS", x => x.VETERINARIAN_ID);
                    table.ForeignKey(
                        name: "FK_VETERINARIANS_SPECIALTIES_SPECIALTY_ID",
                        column: x => x.SPECIALTY_ID,
                        principalTable: "SPECIALTIES",
                        principalColumn: "SPECIALTY_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VETERINARIANS_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VETERINARIANS_LICENSE_NUMBER",
                table: "VETERINARIANS",
                column: "LICENSE_NUMBER",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VETERINARIANS_SPECIALTY_ID",
                table: "VETERINARIANS",
                column: "SPECIALTY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_VETERINARIANS_USER_ID",
                table: "VETERINARIANS",
                column: "USER_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VETERINARIANS");
        }
    }
}
