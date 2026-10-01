using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationStayPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IS_PAID",
                table: "HOSPITALIZATION_STAYS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PAID_AT",
                table: "HOSPITALIZATION_STAYS",
                type: "TIMESTAMP",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IS_PAID",
                table: "HOSPITALIZATION_STAYS");

            migrationBuilder.DropColumn(
                name: "PAID_AT",
                table: "HOSPITALIZATION_STAYS");
        }
    }
}
