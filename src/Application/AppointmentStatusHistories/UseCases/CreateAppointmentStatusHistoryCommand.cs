using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed record CreateAppointmentStatusHistoryCommand(
    Guid AppointmentId,
    Guid StatusId,
    Guid ClientPetId,
    string? Comment) : IRequest<Guid>;
