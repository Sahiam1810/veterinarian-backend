using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilitiesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AVAILABILITIES",
                columns: table => new
                {
                    AVAILABILITY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    VETERINARIAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    DAY_OF_WEEK = table.Column<decimal>(type: "NUMBER", nullable: false),
                    START_TIME = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    END_TIME = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    IS_ACTIVE = table.Column<string>(type: "CHAR(1)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AVAILABILITIES", x => x.AVAILABILITY_ID);
                    table.ForeignKey(
                        name: "FK_AVAILABILITIES_VETERINARIANS_VETERINARIAN_ID",
                        column: x => x.VETERINARIAN_ID,
                        principalTable: "VETERINARIANS",
                        principalColumn: "VETERINARIAN_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AVAILABILITIES_VETERINARIAN_ID",
                table: "AVAILABILITIES",
                column: "VETERINARIAN_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AVAILABILITIES");
        }
    }
}
