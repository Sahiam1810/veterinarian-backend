using Api.Veterinarians.Dtos;
using Application.Veterinarians.UseCases;
using Domain.Veterinarians.Entities;

namespace Api.Veterinarians.Mappings;

public static class VeterinarianMappings
{
    public static CreateVeterinarianCommand ToCommand(
        this CreateVeterinarianRequest request)
    {
        return new CreateVeterinarianCommand(
            request.UserId,
            request.SpecialtyId,
            request.LicenseNumber);
    }

    public static UpdateVeterinarianCommand ToCommand(
        this UpdateVeterinarianRequest request,
        Guid id)
    {
        return new UpdateVeterinarianCommand(
            id,
            request.UserId,
            request.SpecialtyId,
            request.LicenseNumber);
    }

    public static VeterinarianResponse ToResponse(
        this Veterinarian entity)
    {
        return new VeterinarianResponse(
            entity.Id,
            entity.UserId,
            entity.User?.FullName,
            entity.SpecialtyId,
            entity.Specialty?.Name.Value,
            entity.LicenseNumber,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<VeterinarianResponse> ToResponse(
        this IReadOnlyCollection<Veterinarian> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
