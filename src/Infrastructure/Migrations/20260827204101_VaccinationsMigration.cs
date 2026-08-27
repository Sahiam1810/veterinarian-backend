using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VaccinationsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VACCINATIONS",
                columns: table => new
                {
                    VACCINATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    RECORD_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    VACCINE_NAME = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    DOSE_NUMBER = table.Column<decimal>(type: "NUMBER", nullable: false),
                    APPLICATION_DATE = table.Column<DateTime>(type: "DATE", nullable: false),
                    NEXT_DOSE_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VACCINATIONS", x => x.VACCINATION_ID);
                    table.ForeignKey(
                        name: "FK_VACCINATIONS_CLIENTS_PETS_CLIENT_PET_ID",
                        column: x => x.CLIENT_PET_ID,
                        principalTable: "CLIENTS_PETS",
                        principalColumn: "CLIENT_PET_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VACCINATIONS_MEDICAL_RECORDS_RECORD_ID",
                        column: x => x.RECORD_ID,
                        principalTable: "MEDICAL_RECORDS",
                        principalColumn: "RECORD_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VACCINATIONS_CLIENT_PET_ID",
                table: "VACCINATIONS",
                column: "CLIENT_PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_VACCINATIONS_RECORD_ID",
                table: "VACCINATIONS",
                column: "RECORD_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VACCINATIONS");
        }
    }
}
