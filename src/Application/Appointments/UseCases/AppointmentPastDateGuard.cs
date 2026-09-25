using Application.Common.Exceptions;

namespace Application.Appointments.UseCases;

// S36: solo para los handlers de staff (Crear/Reprogramar cita); el flujo de
// autoservicio del cliente (chatbot/Telegram) tiene su propia validación
// (CreateMyAppointmentCommand.ValidateBookingWindow) y no usa esta clase.
internal static class AppointmentPastDateGuard
{
    public static void EnsureNotInThePast(DateTime scheduledStart, TimeProvider timeProvider)
    {
        var startUtc = scheduledStart.Kind == DateTimeKind.Utc
            ? scheduledStart
            : DateTime.SpecifyKind(scheduledStart, DateTimeKind.Utc);

        if (startUtc < timeProvider.GetUtcNow().UtcDateTime)
        {
            throw new BadRequestException(
                "No se puede agendar ni reprogramar una cita en una fecha u hora que ya pasó.");
        }
    }
}
