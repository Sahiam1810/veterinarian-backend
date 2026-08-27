using Api.AiRunStatuses.Dtos;
using Domain.AiRunStatuses.Entities;

namespace Api.AiRunStatuses.Mappings;

public static class AiRunStatusMappings
{
    public static AiRunStatusResponseDto ToDto(this AiRunStatusEntity entity) => new(entity.Id, entity.NameStatus.Value, entity.CreatedAt, entity.UpdatedAt);
}
