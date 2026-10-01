using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalizationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HOSPITALIZATION_SETTINGS",
                columns: table => new
                {
                    SETTINGS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    DAILY_RATE = table.Column<decimal>(type: "NUMBER(18,2)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOSPITALIZATION_SETTINGS", x => x.SETTINGS_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HOSPITALIZATION_SETTINGS");
        }
    }
}
