using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TypeServicesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TYPE_SERVICES",
                columns: table => new
                {
                    TYPE_SERVICE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(200)", maxLength: 200, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TYPE_SERVICES", x => x.TYPE_SERVICE_ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TYPE_SERVICES_NAME",
                table: "TYPE_SERVICES",
                column: "NAME",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TYPE_SERVICES");
        }
    }
}
