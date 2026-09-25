using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationStaysAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HOSPITALIZATION_STAYS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    ADMITTED_BY_USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    FECHA_INGRESO = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    FECHA_ALTA = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    ESTADO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MOTIVO = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOSPITALIZATION_STAYS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HOSPITALIZATION_NOTES",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    STAY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AUTOR_USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    FECHA_HORA = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    NOTA = table.Column<string>(type: "VARCHAR2(2000)", maxLength: 2000, nullable: false),
                    ENTREGADO_A_USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOSPITALIZATION_NOTES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_HOSPITALIZATION_NOTES_STAY",
                        column: x => x.STAY_ID,
                        principalTable: "HOSPITALIZATION_STAYS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HOSP_NOTE_STAY_ID",
                table: "HOSPITALIZATION_NOTES",
                column: "STAY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_HOSP_STAY_ACTIVE_PER_PET",
                table: "HOSPITALIZATION_STAYS",
                columns: new[] { "CLIENT_PET_ID", "ESTADO" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HOSPITALIZATION_NOTES");

            migrationBuilder.DropTable(
                name: "HOSPITALIZATION_STAYS");
        }
    }
}
