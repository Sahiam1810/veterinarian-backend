using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFrozenPricesToOrdersAndAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UNIT_PRICE",
                table: "PROCEDURE_ORDER_ITEMS",
                type: "NUMBER(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UNIT_PRICE",
                table: "MEDICATION_ORDER_ITEMS",
                type: "NUMBER(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PAID_AMOUNT",
                table: "APPOINTMENTS",
                type: "NUMBER(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UNIT_PRICE",
                table: "PROCEDURE_ORDER_ITEMS");

            migrationBuilder.DropColumn(
                name: "UNIT_PRICE",
                table: "MEDICATION_ORDER_ITEMS");

            migrationBuilder.DropColumn(
                name: "PAID_AMOUNT",
                table: "APPOINTMENTS");
        }
    }
}
