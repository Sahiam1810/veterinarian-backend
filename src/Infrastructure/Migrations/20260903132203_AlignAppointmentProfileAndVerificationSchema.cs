using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    // No-op: el esquema PHONE_NUMBER / OTP ya lo crea AddAppointmentClientOtpSelfService.
    // Mantener el Id en el historial evita ORA-01430 en database update (columna/tabla duplicada).
    public partial class AlignAppointmentProfileAndVerificationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vacío: dueña = 20260903030852_AddAppointmentClientOtpSelfService.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vacío: no revertir lo creado por AddAppointmentClientOtpSelfService.
        }
    }
}
