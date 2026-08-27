using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed record UpdateAppointmentStatusHistoryCommand(
    Guid Id,
    Guid AppointmentId,
    Guid StatusId,
    Guid ClientPetId,
    string? Comment) : IRequest<bool>;
