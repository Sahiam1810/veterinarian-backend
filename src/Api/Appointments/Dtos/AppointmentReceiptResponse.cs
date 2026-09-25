namespace Api.Appointments.Dtos;

public sealed record AppointmentReceiptResponse(
    string PetName,
    string OwnerName,
    string? OwnerPhone,
    string ServiceName,
    decimal ServicePrice,
    DateTime ScheduledStart,
    bool IsPaid);
