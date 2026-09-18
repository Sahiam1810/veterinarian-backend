using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record CreateAvailabilityCommand(
    Guid VeterinarianId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive,
    int SlotDurationMinutes = Availability.DefaultSlotDurationMinutes,
    string? ShiftName = null,
    string? ConsultingRoom = null,
    int MaxConcurrentAppointments = Availability.DefaultMaxConcurrentAppointments) : IRequest<Guid>;
