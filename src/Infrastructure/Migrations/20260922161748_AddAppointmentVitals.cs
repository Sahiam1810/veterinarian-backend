using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentVitals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "HEART_RATE",
                table: "APPOINTMENTS",
                type: "NUMBER(5)",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "RESPIRATORY_RATE",
                table: "APPOINTMENTS",
                type: "NUMBER(5)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TEMPERATURE",
                table: "APPOINTMENTS",
                type: "NUMBER(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WEIGHT",
                table: "APPOINTMENTS",
                type: "NUMBER(8,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HEART_RATE",
                table: "APPOINTMENTS");

            migrationBuilder.DropColumn(
                name: "RESPIRATORY_RATE",
                table: "APPOINTMENTS");

            migrationBuilder.DropColumn(
                name: "TEMPERATURE",
                table: "APPOINTMENTS");

            migrationBuilder.DropColumn(
                name: "WEIGHT",
                table: "APPOINTMENTS");
        }
    }
}
