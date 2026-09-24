using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationStayIdToOrdersAndUniqueActiveStayIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MEDICATION_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropForeignKey(
                name: "FK_PROCEDURE_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_HOSP_STAY_ACTIVE_PER_PET",
                table: "HOSPITALIZATION_STAYS");

            migrationBuilder.AlterColumn<string>(
                name: "APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS",
                type: "VARCHAR2(36)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)");

            migrationBuilder.AddColumn<string>(
                name: "HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "APPOINTMENT_ID",
                table: "MEDICATION_ORDERS",
                type: "VARCHAR2(36)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)");

            migrationBuilder.AddColumn<string>(
                name: "HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROCEDURE_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS",
                column: "HOSPITALIZATION_STAY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATION_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS",
                column: "HOSPITALIZATION_STAY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_HOSP_STAY_ACTIVE_PER_PET",
                table: "HOSPITALIZATION_STAYS",
                columns: new[] { "CLIENT_PET_ID", "ESTADO" },
                unique: true,
                filter: "ESTADO = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_MEDICATION_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "MEDICATION_ORDERS",
                column: "APPOINTMENT_ID",
                principalTable: "APPOINTMENTS",
                principalColumn: "APPOINTMENT_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_MEDICATION_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS",
                column: "HOSPITALIZATION_STAY_ID",
                principalTable: "HOSPITALIZATION_STAYS",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_PROCEDURE_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS",
                column: "APPOINTMENT_ID",
                principalTable: "APPOINTMENTS",
                principalColumn: "APPOINTMENT_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_PROCEDURE_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS",
                column: "HOSPITALIZATION_STAY_ID",
                principalTable: "HOSPITALIZATION_STAYS",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MEDICATION_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropForeignKey(
                name: "FK_MEDICATION_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropForeignKey(
                name: "FK_PROCEDURE_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropForeignKey(
                name: "FK_PROCEDURE_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_PROCEDURE_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_MEDICATION_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_HOSP_STAY_ACTIVE_PER_PET",
                table: "HOSPITALIZATION_STAYS");

            migrationBuilder.DropColumn(
                name: "HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropColumn(
                name: "HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.AlterColumn<string>(
                name: "APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS",
                type: "VARCHAR2(36)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "APPOINTMENT_ID",
                table: "MEDICATION_ORDERS",
                type: "VARCHAR2(36)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR2(36)",
                oldNullable: true);

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
