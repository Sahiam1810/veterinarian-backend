using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ValidateHospitalizationTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IX_HOSP_STAY_ACTIVE_PER_PET
                ON HOSPITALIZATION_STAYS (
                    CASE WHEN ESTADO = 0 THEN CLIENT_PET_ID END
                )
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MEDICATION_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "MEDICATION_ORDERS",
                column: "APPOINTMENT_ID",
                principalTable: "APPOINTMENTS",
                principalColumn: "APPOINTMENT_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_PROCEDURE_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS",
                column: "APPOINTMENT_ID",
                principalTable: "APPOINTMENTS",
                principalColumn: "APPOINTMENT_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MEDICATION_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropForeignKey(
                name: "FK_PROCEDURE_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.Sql(
                "DROP INDEX IX_HOSP_STAY_ACTIVE_PER_PET");

            migrationBuilder.CreateIndex(
                name: "IX_HOSP_STAY_ACTIVE_PER_PET",
                table: "HOSPITALIZATION_STAYS",
                columns: new[] { "CLIENT_PET_ID", "ESTADO" });

            migrationBuilder.AddForeignKey(
                name: "FK_MEDICATION_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "MEDICATION_ORDERS",
                column: "APPOINTMENT_ID",
                principalTable: "APPOINTMENTS",
                principalColumn: "APPOINTMENT_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PROCEDURE_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS",
                column: "APPOINTMENT_ID",
                principalTable: "APPOINTMENTS",
                principalColumn: "APPOINTMENT_ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
