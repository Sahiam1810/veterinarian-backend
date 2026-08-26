using Api.Specialties.Dtos;
using Domain.Specialties.Entities;

namespace Api.Specialties.Mappings;

public static class SpecialtyMappings
{
    public static SpecialtyResponseDto ToDto(this SpecialtyEntity entity) => new(entity.Id, entity.Name.Value, entity.Description.Value, entity.CreatedAt, entity.UpdatedAt);
}
