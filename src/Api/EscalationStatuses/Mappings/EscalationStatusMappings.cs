using Api.EscalationStatuses.Dtos;
using Domain.EscalationStatuses.Entities;

namespace Api.EscalationStatuses.Mappings;

// Mapeos de entidad a DTO de respuesta.
public static class EscalationStatusMappings
{
    public static EscalationStatusResponseDto ToDto(this EscalationStatusEntity entity) =>
        new(entity.Id, entity.Name.Value, entity.CreatedAt, entity.UpdatedAt);
}
