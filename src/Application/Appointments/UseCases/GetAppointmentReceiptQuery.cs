using MediatR;

namespace Application.Appointments.UseCases;

public sealed record AppointmentReceiptResult(
    string PetName,
    string OwnerName,
    string? OwnerPhone,
    string ServiceName,
    decimal ServicePrice,
    DateTime ScheduledStart,
    bool IsPaid);

public sealed record GetAppointmentReceiptQuery(Guid AppointmentId) : IRequest<AppointmentReceiptResult>;
