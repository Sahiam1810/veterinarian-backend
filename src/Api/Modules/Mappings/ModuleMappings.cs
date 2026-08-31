using Api.Modules.Dtos;
using Domain.Modules.Entities;

namespace Api.Modules.Mappings;


public static class ModuleMappings
{
    public static ModuleResponseDto ToDto(this ModuleEntity entity)
        => new(
            entity.Id,
            entity.Name.Value,
            entity.Description,
            entity.CreatedAt,
            entity.UpdatedAt);
=======

public static class ModuleMappings
{
    public static ModuleResponseDto ToDto(this ModuleEntity entity) =>
        new(entity.Id, entity.Name.Value, entity.Description, entity.CreatedAt, entity.UpdatedAt);

}
