using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentClientOtpSelfService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PHONE_NUMBER",
                table: "CLIENTS",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "REQUESTER_PHONE_NUMBER",
                table: "APPOINTMENTS",
                type: "VARCHAR2(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACTION = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    CHANNEL = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    DESTINATION_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: false),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    ACTION_PAYLOAD = table.Column<string>(type: "VARCHAR2(1000)", maxLength: 1000, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APPOINTMENT_ACTION_VERIFICATION_SESSIONS", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APPT_ACTION_VERIF_ACTIVE",
                table: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS",
                columns: new[] { "APPOINTMENT_ID", "ACTION", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_APPT_ACTION_VERIF_DEST",
                table: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS",
                column: "DESTINATION_HASH");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS");

            migrationBuilder.DropColumn(
                name: "PHONE_NUMBER",
                table: "CLIENTS");

            migrationBuilder.DropColumn(
                name: "REQUESTER_PHONE_NUMBER",
                table: "APPOINTMENTS");
        }
    }
}
