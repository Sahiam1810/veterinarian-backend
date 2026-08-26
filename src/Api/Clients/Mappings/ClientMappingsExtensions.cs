using Api.Clients.Dtos;
using Domain.Clients.Entities;

namespace Api.Clients.Mappings;

public static class ClientMappingsExtensions
{
    public static ClientResponseDto ToDto(this ClientEntity entity)
    {
        return new ClientResponseDto(
            entity.Id,
            entity.UserId,
            entity.IdentificationNumber.Value,
            entity.Address?.Value,
            entity.RegistrationDate,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }
}
