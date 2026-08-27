using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppoimentsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APPOINTMENTS",
                columns: table => new
                {
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    VETERINARIAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    SERVICE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    STATUS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AVAILABILITY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    SCHEDULED_START = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    SCHEDULED_END = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    NOTES = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APPOINTMENTS", x => x.APPOINTMENT_ID);
                    table.ForeignKey(
                        name: "FK_APPOINTMENTS_AVAILABILITIES_AVAILABILITY_ID",
                        column: x => x.AVAILABILITY_ID,
                        principalTable: "AVAILABILITIES",
                        principalColumn: "AVAILABILITY_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APPOINTMENTS_CLIENTS_PETS_CLIENT_PET_ID",
                        column: x => x.CLIENT_PET_ID,
                        principalTable: "CLIENTS_PETS",
                        principalColumn: "CLIENT_PET_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APPOINTMENTS_SERVICES_SERVICE_ID",
                        column: x => x.SERVICE_ID,
                        principalTable: "SERVICES",
                        principalColumn: "SERVICE_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APPOINTMENTS_STATUS_APPOINTMENTS_STATUS_ID",
                        column: x => x.STATUS_ID,
                        principalTable: "STATUS_APPOINTMENTS",
                        principalColumn: "STATUS_APPOINTMENT_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APPOINTMENTS_VETERINARIANS_VETERINARIAN_ID",
                        column: x => x.VETERINARIAN_ID,
                        principalTable: "VETERINARIANS",
                        principalColumn: "VETERINARIAN_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENTS_AVAILABILITY_ID",
                table: "APPOINTMENTS",
                column: "AVAILABILITY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENTS_CLIENT_PET_ID",
                table: "APPOINTMENTS",
                column: "CLIENT_PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENTS_SERVICE_ID",
                table: "APPOINTMENTS",
                column: "SERVICE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENTS_STATUS_ID",
                table: "APPOINTMENTS",
                column: "STATUS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENTS_VETERINARIAN_ID",
                table: "APPOINTMENTS",
                column: "VETERINARIAN_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APPOINTMENTS");
        }
    }
}
