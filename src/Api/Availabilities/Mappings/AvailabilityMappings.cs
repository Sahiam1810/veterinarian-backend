using Api.Availabilities.Dtos;
using Application.Availabilities.UseCase;
using Domain.Availabilities.Entities;

namespace Api.Availabilities.Mappings;

public static class AvailabilityMappings
{
    public static CreateAvailabilityCommand ToCommand(
        this CreateAvailabilityRequest request)
    {
        return new CreateAvailabilityCommand(
            request.VeterinarianId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.IsActive,
            request.SlotDurationMinutes,
            request.ShiftName,
            request.ConsultingRoom,
            request.MaxConcurrentAppointments);
    }

    public static UpdateAvailabilityCommand ToCommand(
        this UpdateAvailabilityRequest request,
        Guid id)
    {
        return new UpdateAvailabilityCommand(
            id,
            request.VeterinarianId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.IsActive,
            request.SlotDurationMinutes,
            request.ShiftName,
            request.ConsultingRoom,
            request.MaxConcurrentAppointments);
    }

    public static AvailabilityResponse ToResponse(
        this Availability entity)
    {
        return new AvailabilityResponse(
            entity.Id,
            entity.VeterinarianId,
            entity.Veterinarian?.LicenseNumber,
            entity.DayOfWeek,
            entity.StartTime,
            entity.EndTime,
            entity.IsActive,
            entity.CreatedAt,
            entity.SlotDurationMinutes,
            entity.ShiftName,
            entity.ConsultingRoom,
            entity.MaxConcurrentAppointments);
    }

    public static IReadOnlyCollection<AvailabilityResponse> ToResponse(
        this IReadOnlyCollection<Availability> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
