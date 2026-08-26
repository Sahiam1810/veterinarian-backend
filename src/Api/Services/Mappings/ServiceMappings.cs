using Api.Services.Dtos;
using Application.Services.UseCases;
using Domain.Services.Entities;

namespace Api.Services.Mappings;

public static class ServiceMappings
{
    public static CreateServiceCommand ToCommand(
        this CreateServiceRequest request)
    {
        return new CreateServiceCommand(
            request.TypeServiceId,
            request.Name,
            request.DurationMinutes,
            request.Price,
            request.IsActive);
    }

    public static UpdateServiceCommand ToCommand(
        this UpdateServiceRequest request,
        Guid id)
    {
        return new UpdateServiceCommand(
            id,
            request.TypeServiceId,
            request.Name,
            request.DurationMinutes,
            request.Price,
            request.IsActive);
    }

    public static ServiceResponse ToResponse(
        this Service entity)
    {
        return new ServiceResponse(
            entity.Id,
            entity.TypeServiceId,
            entity.TypeService?.Name,
            entity.Name,
            entity.DurationMinutes,
            entity.Price,
            entity.IsActive,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<ServiceResponse> ToResponse(
        this IReadOnlyCollection<Service> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
