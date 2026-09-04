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

    // Lookup anónimo por teléfono: mismo recorte de PII que by-identification.
    public static ClientPhoneLookupResponseDto ToPhoneLookupResponse(this ClientEntity entity)
    {
        return new ClientPhoneLookupResponseDto(
            entity.Id,
            entity.UserId,
            entity.IdentificationNumber.Value,
            entity.RegistrationDate
        );
    }
}
