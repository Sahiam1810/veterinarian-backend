using Api.ClientsPets.Dtos;
using Domain.ClientsPets.Entities;

namespace Api.ClientsPets.Mappings;

public static class ClientPetMappings
{
    public static ClientPetResponseDto ToDto(this ClientPetEntity entity) => new(entity.Id, entity.ClientId, entity.PetId, entity.IsPrimaryOwner.Value, entity.CreatedAt, entity.UpdatedAt);
}
