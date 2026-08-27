using Api.MessageTypes.Dtos;
using Domain.MessageTypes.Entities;

namespace Api.MessageTypes.Mappings;

public static class MessageTypeMappings
{
    public static MessageTypeResponseDto ToDto(this MessageTypeEntity entity) =>
        new(entity.Id, entity.Name.Value, entity.CreatedAt, entity.UpdatedAt);
}
