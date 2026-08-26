using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialtiesAndClientsPets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CLIENTS_PETS",
                columns: table => new
                {
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    IS_PRIMARY_OWNER = table.Column<string>(type: "CHAR(1)", maxLength: 1, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIENTS_PETS", x => x.CLIENT_PET_ID);
                    table.ForeignKey(
                        name: "FK_CLIENTS_PETS_CLIENTS_CLIENT_ID",
                        column: x => x.CLIENT_ID,
                        principalTable: "CLIENTS",
                        principalColumn: "CLIENT_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CLIENTS_PETS_PETS_PET_ID",
                        column: x => x.PET_ID,
                        principalTable: "PETS",
                        principalColumn: "PET_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SPECIALTIES",
                columns: table => new
                {
                    SPECIALTY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SPECIALTIES", x => x.SPECIALTY_ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_PETS_CLIENT_ID_PET_ID",
                table: "CLIENTS_PETS",
                columns: new[] { "CLIENT_ID", "PET_ID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_PETS_PET_ID",
                table: "CLIENTS_PETS",
                column: "PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SPECIALTIES_NAME",
                table: "SPECIALTIES",
                column: "NAME",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CLIENTS_PETS");

            migrationBuilder.DropTable(
                name: "SPECIALTIES");
        }
    }
}
