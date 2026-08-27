using Api.ConversationStatuses.Dtos;
using Domain.ConversationStatuses.Entities;

namespace Api.ConversationStatuses.Mappings;

// Mapeos de entidad a DTO de respuesta.
public static class ConversationStatusMappings
{
    public static ConversationStatusResponseDto ToDto(this ConversationStatusEntity entity) =>
        new(entity.Id, entity.Name.Value, entity.CreatedAt, entity.UpdatedAt);
}
