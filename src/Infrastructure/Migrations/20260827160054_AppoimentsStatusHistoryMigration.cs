using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppoimentsStatusHistoryMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APPOINTMENT_STATUS_HISTORIES",
                columns: table => new
                {
                    APPOINTMENT_STATUS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    STATUS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    COMMENT = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APPOINTMENT_STATUS_HISTORIES", x => x.APPOINTMENT_STATUS_ID);
                    table.ForeignKey(
                        name: "FK_APPOINTMENT_STATUS_HISTORIES_APPOINTMENTS_APPOINTMENT_ID",
                        column: x => x.APPOINTMENT_ID,
                        principalTable: "APPOINTMENTS",
                        principalColumn: "APPOINTMENT_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APPOINTMENT_STATUS_HISTORIES_CLIENTS_PETS_CLIENT_PET_ID",
                        column: x => x.CLIENT_PET_ID,
                        principalTable: "CLIENTS_PETS",
                        principalColumn: "CLIENT_PET_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APPOINTMENT_STATUS_HISTORIES_STATUS_APPOINTMENTS_STATUS_ID",
                        column: x => x.STATUS_ID,
                        principalTable: "STATUS_APPOINTMENTS",
                        principalColumn: "STATUS_APPOINTMENT_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENT_STATUS_HISTORIES_APPOINTMENT_ID",
                table: "APPOINTMENT_STATUS_HISTORIES",
                column: "APPOINTMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENT_STATUS_HISTORIES_CLIENT_PET_ID",
                table: "APPOINTMENT_STATUS_HISTORIES",
                column: "CLIENT_PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_APPOINTMENT_STATUS_HISTORIES_STATUS_ID",
                table: "APPOINTMENT_STATUS_HISTORIES",
                column: "STATUS_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APPOINTMENT_STATUS_HISTORIES");
        }
    }
}
