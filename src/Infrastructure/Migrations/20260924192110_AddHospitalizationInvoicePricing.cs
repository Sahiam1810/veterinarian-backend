using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationInvoicePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PRICE",
                table: "PROCEDURES",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE",
                table: "MEDICATIONS",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS",
                type: "VARCHAR2(36)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DAILY_RATE",
                table: "HOSPITALIZATION_STAYS",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_PROCEDURE_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS",
                column: "HOSPITALIZATION_STAY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATION_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS",
                column: "HOSPITALIZATION_STAY_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_MEDICATION_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS",
                column: "HOSPITALIZATION_STAY_ID",
                principalTable: "HOSPITALIZATION_STAYS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PROCEDURE_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS",
                column: "HOSPITALIZATION_STAY_ID",
                principalTable: "HOSPITALIZATION_STAYS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MEDICATION_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropForeignKey(
                name: "FK_PROCEDURE_ORDERS_HOSPITALIZATION_STAYS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_PROCEDURE_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_MEDICATION_ORDERS_HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropColumn(
                name: "PRICE",
                table: "PROCEDURES");

            migrationBuilder.DropColumn(
                name: "HOSPITALIZATION_STAY_ID",
                table: "PROCEDURE_ORDERS");

            migrationBuilder.DropColumn(
                name: "PRICE",
                table: "MEDICATIONS");

            migrationBuilder.DropColumn(
                name: "HOSPITALIZATION_STAY_ID",
                table: "MEDICATION_ORDERS");

            migrationBuilder.DropColumn(
                name: "DAILY_RATE",
                table: "HOSPITALIZATION_STAYS");
        }
    }
}
