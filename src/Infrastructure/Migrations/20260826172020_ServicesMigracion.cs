using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServicesMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SERVICES",
                columns: table => new
                {
                    SERVICE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    TYPE_SERVICE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    DURATION_MINUTES = table.Column<decimal>(type: "NUMBER", nullable: false),
                    PRICE = table.Column<decimal>(type: "NUMBER", nullable: false),
                    IS_ACTIVE = table.Column<string>(type: "CHAR(1)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SERVICES", x => x.SERVICE_ID);
                    table.ForeignKey(
                        name: "FK_SERVICES_TYPE_SERVICES_TYPE_SERVICE_ID",
                        column: x => x.TYPE_SERVICE_ID,
                        principalTable: "TYPE_SERVICES",
                        principalColumn: "TYPE_SERVICE_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SERVICES_NAME",
                table: "SERVICES",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SERVICES_TYPE_SERVICE_ID",
                table: "SERVICES",
                column: "TYPE_SERVICE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SERVICES");
        }
    }
}
