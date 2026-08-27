using Api.Priorities.Dtos;
using Domain.Priorities.Entities;

namespace Api.Priorities.Mappings;

// Mapeos de entidad a DTO de respuesta.
public static class PriorityMappings
{
    public static PriorityResponseDto ToDto(this PriorityEntity entity) =>
        new(entity.Id, entity.Name.Value, entity.CreatedAt, entity.UpdatedAt);
}
