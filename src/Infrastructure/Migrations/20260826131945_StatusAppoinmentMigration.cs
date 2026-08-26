using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StatusAppoinmentMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "STATUS_APPOINTMENTS",
                columns: table => new
                {
                    STATUS_APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(200)", maxLength: 200, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STATUS_APPOINTMENTS", x => x.STATUS_APPOINTMENT_ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_STATUS_APPOINTMENTS_NAME",
                table: "STATUS_APPOINTMENTS",
                column: "NAME",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "STATUS_APPOINTMENTS");
        }
    }
}
