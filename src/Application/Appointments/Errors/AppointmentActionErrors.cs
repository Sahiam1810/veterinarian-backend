using Application.Common.Results;

namespace Application.Appointments.Errors;

// OTP de accion de cita (cancelar/reagendar). No es Gmail ni ContactVerification.
// Telegram traduce por code; no hay OTP en la web staff.
public static class AppointmentActionErrors
{
    private const string GenericDescription = "Appointment action verification failed.";

    // Telefono no coincide con RequesterPhoneNumber de la cita / sesion.
    public static readonly Error PhoneMismatch = new(
        "AppointmentAction.PhoneMismatch",
        GenericDescription);

    public static readonly Error InvalidCode = new(
        "AppointmentAction.InvalidCode",
        GenericDescription);

    public static readonly Error Expired = new(
        "AppointmentAction.Expired",
        GenericDescription);

    public static readonly Error AttemptsExhausted = new(
        "AppointmentAction.AttemptsExhausted",
        GenericDescription);

    public static readonly Error ResendTooSoon = new(
        "AppointmentAction.ResendTooSoon",
        GenericDescription);

    public static readonly Error DeliveryFailed = new(
        "AppointmentAction.DeliveryFailed",
        GenericDescription);

    public static readonly Error SessionNotFound = new(
        "AppointmentAction.SessionNotFound",
        GenericDescription);

    public static readonly Error InvalidAction = new(
        "AppointmentAction.InvalidAction",
        GenericDescription);
}
