using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotificationsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NOTIFICATIONS",
                columns: table => new
                {
                    NOTIFICATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    MESSAGE = table.Column<string>(type: "VARCHAR2(1000)", maxLength: 1000, nullable: false),
                    SENT_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    TYPE = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATE_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATIONS", x => x.NOTIFICATION_ID);
                    table.ForeignKey(
                        name: "FK_NOTIFICATIONS_APPOINTMENTS_APPOINTMENT_ID",
                        column: x => x.APPOINTMENT_ID,
                        principalTable: "APPOINTMENTS",
                        principalColumn: "APPOINTMENT_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NOTIFICATIONS_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_APPOINTMENT_ID",
                table: "NOTIFICATIONS",
                column: "APPOINTMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_USER_ID",
                table: "NOTIFICATIONS",
                column: "USER_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NOTIFICATIONS");
        }
    }
}
