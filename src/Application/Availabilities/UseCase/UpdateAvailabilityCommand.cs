using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record UpdateAvailabilityCommand(
    Guid Id,
    Guid VeterinarianId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive) : IRequest;
