using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentBookingIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BOOKING_REQUEST_KEY_HASH",
                table: "APPOINTMENTS",
                type: "VARCHAR2(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_APPTS_BOOKING_REQ_HASH",
                table: "APPOINTMENTS",
                column: "BOOKING_REQUEST_KEY_HASH",
                unique: true,
                filter: "\"BOOKING_REQUEST_KEY_HASH\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_APPTS_BOOKING_REQ_HASH",
                table: "APPOINTMENTS");

            migrationBuilder.DropColumn(
                name: "BOOKING_REQUEST_KEY_HASH",
                table: "APPOINTMENTS");
        }
    }
}
