using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleConsultingRoomAndAbsences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CONSULTING_ROOM",
                table: "AVAILABILITIES",
                type: "VARCHAR2(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MAX_CONCURRENT_APPOINTMENTS",
                table: "AVAILABILITIES",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SHIFT_NAME",
                table: "AVAILABILITIES",
                type: "VARCHAR2(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SLOT_DURATION_MINUTES",
                table: "AVAILABILITIES",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "CONSULTING_ROOM",
                table: "APPOINTMENTS",
                type: "VARCHAR2(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VETERINARIAN_ABSENCES",
                columns: table => new
                {
                    ABSENCE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    VETERINARIAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    START_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    END_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    REASON = table.Column<string>(type: "VARCHAR2(200)", maxLength: 200, nullable: true),
                    IS_FULL_DAY = table.Column<string>(type: "CHAR(1)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VETERINARIAN_ABSENCES", x => x.ABSENCE_ID);
                    table.ForeignKey(
                        name: "FK_VETERINARIAN_ABSENCES_VETERINARIANS_VETERINARIAN_ID",
                        column: x => x.VETERINARIAN_ID,
                        principalTable: "VETERINARIANS",
                        principalColumn: "VETERINARIAN_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VETERINARIAN_ABSENCES_VETERINARIAN_ID",
                table: "VETERINARIAN_ABSENCES",
                column: "VETERINARIAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_VETERINARIAN_ABSENCES_VETERINARIAN_ID_START_AT_END_AT",
                table: "VETERINARIAN_ABSENCES",
                columns: new[] { "VETERINARIAN_ID", "START_AT", "END_AT" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VETERINARIAN_ABSENCES");

            migrationBuilder.DropColumn(
                name: "CONSULTING_ROOM",
                table: "AVAILABILITIES");

            migrationBuilder.DropColumn(
                name: "MAX_CONCURRENT_APPOINTMENTS",
                table: "AVAILABILITIES");

            migrationBuilder.DropColumn(
                name: "SHIFT_NAME",
                table: "AVAILABILITIES");

            migrationBuilder.DropColumn(
                name: "SLOT_DURATION_MINUTES",
                table: "AVAILABILITIES");

            migrationBuilder.DropColumn(
                name: "CONSULTING_ROOM",
                table: "APPOINTMENTS");
        }
    }
}
