using Api.Clients.Dtos;
using Application.Clients.UseCases;
using Domain.Clients.Entities;

namespace Api.Clients.Mappings;

public static class ClientMappingsExtensions
{
    public static CreateClientCommand ToCommand(this CreateClientDto dto) =>
        new(
            dto.UserId,
            dto.IdentificationNumber,
            dto.Address,
            dto.RegistrationDate,
            dto.PhoneNumber);

    public static UpdateClientCommand ToCommand(this UpdateClientDto dto, Guid id) =>
        new(
            id,
            dto.UserId,
            dto.IdentificationNumber,
            dto.Address,
            dto.RegistrationDate,
            dto.PhoneNumber);

    public static ClientResponseDto ToDto(this ClientEntity entity)
    {
        return new ClientResponseDto(
            entity.Id,
            entity.UserId,
            entity.IdentificationNumber.Value,
            entity.Address?.Value,
            entity.PhoneNumber?.Value,
            entity.RegistrationDate,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }

    public static ClientIdentificationLookupResponseDto ToIdentificationLookupResponse(this ClientEntity entity)
    {
        return new ClientIdentificationLookupResponseDto(
            entity.Id,
            entity.UserId,
            entity.IdentificationNumber.Value,
            entity.RegistrationDate
        );
    }
}
