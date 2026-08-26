using Api.SenderTypes.Dtos;
using Domain.SenderTypes.Entities;

namespace Api.SenderTypes.Mappings;

public static class SenderTypeMappings
{
    public static SenderTypeResponseDto ToDto(this SenderTypeEntity entity) => new(entity.Id, entity.Name.Value, entity.CreatedAt, entity.UpdatedAt);
}
