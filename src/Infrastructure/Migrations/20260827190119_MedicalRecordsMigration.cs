using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MedicalRecordsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MEDICAL_RECORDS",
                columns: table => new
                {
                    RECORD_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    DIAGNOSTIC_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    SYMPTOMS = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: true),
                    TREATMENT = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: true),
                    WEIGHT_AT_VISIT = table.Column<decimal>(type: "NUMBER", nullable: true),
                    TEMPERATURE = table.Column<decimal>(type: "NUMBER", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICAL_RECORDS", x => x.RECORD_ID);
                    table.ForeignKey(
                        name: "FK_MEDICAL_RECORDS_APPOINTMENTS_APPOINTMENT_ID",
                        column: x => x.APPOINTMENT_ID,
                        principalTable: "APPOINTMENTS",
                        principalColumn: "APPOINTMENT_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MEDICAL_RECORDS_CLIENTS_PETS_CLIENT_PET_ID",
                        column: x => x.CLIENT_PET_ID,
                        principalTable: "CLIENTS_PETS",
                        principalColumn: "CLIENT_PET_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MEDICAL_RECORDS_DIAGNOSTICS_DIAGNOSTIC_ID",
                        column: x => x.DIAGNOSTIC_ID,
                        principalTable: "DIAGNOSTICS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MEDICAL_RECORDS_APPOINTMENT_ID",
                table: "MEDICAL_RECORDS",
                column: "APPOINTMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICAL_RECORDS_CLIENT_PET_ID",
                table: "MEDICAL_RECORDS",
                column: "CLIENT_PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICAL_RECORDS_DIAGNOSTIC_ID",
                table: "MEDICAL_RECORDS",
                column: "DIAGNOSTIC_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MEDICAL_RECORDS");
        }
    }
}
