using MediatR;

namespace Application.Appointments.UseCases;

public sealed record RecordAppointmentVitalsCommand(
    Guid AppointmentId,
    decimal? Weight,
    decimal? Temperature,
    int? HeartRate,
    int? RespiratoryRate) : IRequest;
