using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record CreateAvailabilityCommand(
    Guid VeterinarianId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive) : IRequest<Guid>;
