using Api.VeterinarianAbsences.Dtos;
using Application.VeterinarianAbsences.UseCases;
using Domain.VeterinarianAbsences.Entities;

namespace Api.VeterinarianAbsences.Mappings;

public static class VeterinarianAbsenceMappings
{
    public static CreateVeterinarianAbsenceCommand ToCommand(
        this CreateVeterinarianAbsenceRequest request)
    {
        return new CreateVeterinarianAbsenceCommand(
            request.VeterinarianId,
            request.StartAtUtc,
            request.EndAtUtc,
            request.Reason,
            request.IsFullDay);
    }

    public static UpdateVeterinarianAbsenceCommand ToCommand(
        this UpdateVeterinarianAbsenceRequest request,
        Guid id)
    {
        return new UpdateVeterinarianAbsenceCommand(
            id,
            request.StartAtUtc,
            request.EndAtUtc,
            request.Reason,
            request.IsFullDay);
    }

    public static VeterinarianAbsenceResponse ToResponse(
        this VeterinarianAbsence entity)
    {
        return new VeterinarianAbsenceResponse(
            entity.Id,
            entity.VeterinarianId,
            AsUtc(entity.StartAtUtc),
            AsUtc(entity.EndAtUtc),
            entity.Reason,
            entity.IsFullDay,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<VeterinarianAbsenceResponse> ToResponse(
        this IReadOnlyCollection<VeterinarianAbsence> entities)
    {
        return entities.Select(item => item.ToResponse()).ToArray();
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
