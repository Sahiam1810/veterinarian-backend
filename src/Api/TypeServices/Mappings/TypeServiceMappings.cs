using Api.TypeServices.Dtos;
using Application.TypeServices.UseCases;
using Domain.TypeServices.Entities;

namespace Api.TypeServices.Mappings;

public static class TypeServiceMappings
{
    public static CreateTypeServiceCommand ToCommand(
        this CreateTypeServiceRequest request)
    {
        return new CreateTypeServiceCommand(
            request.Name,
            request.Description);
    }

    public static UpdateTypeServiceCommand ToCommand(
        this UpdateTypeServiceRequest request,
        Guid id)
    {
        return new UpdateTypeServiceCommand(
            id,
            request.Name,
            request.Description);
    }

    public static TypeServiceResponse ToResponse(
        this TypeService entity)
    {
        return new TypeServiceResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<TypeServiceResponse> ToResponse(
        this IReadOnlyCollection<TypeService> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
