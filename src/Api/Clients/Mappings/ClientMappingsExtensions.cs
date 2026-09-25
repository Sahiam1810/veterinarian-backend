using Api.Clients.Dtos;
using Application.Clients.UseCases;
using Domain.Clients.Entities;

namespace Api.Clients.Mappings;

public static class ClientMappingsExtensions
{
    public static CreateClientCommand ToCommand(this CreateClientDto dto) =>
        new(
            dto.FullName,
            dto.Email,
            dto.IdentificationNumber,
            dto.PhoneNumber ?? string.Empty,
            dto.Address);

    public static UpdateClientCommand ToCommand(this UpdateClientDto dto, Guid id) =>
        new(
            id,
            dto.FullName,
            dto.Email,
            dto.IdentificationNumber,
            dto.PhoneNumber ?? string.Empty,
            dto.Address,
            dto.IsActive ?? throw new ArgumentException("El estado del cliente es obligatorio.", nameof(dto)));

    public static ClientResponseDto ToDto(this ClientEntity entity) =>
        new(
            entity.Id,
            entity.FullName.Value,
            entity.Email?.Value,
            entity.IsActive,
            entity.IdentificationNumber?.Value,
            entity.PhoneNumber?.Value,
            entity.Address?.Value,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static ClientIdentificationLookupResponseDto ToIdentificationLookupResponse(this ClientEntity entity) =>
        new(
            entity.Id,
            entity.IdentificationNumber?.Value,
            entity.CreatedAt);

    public static ClientPhoneLookupResponseDto ToPhoneLookupResponse(this ClientEntity entity) =>
        new(
            entity.Id,
            entity.IdentificationNumber?.Value,
            entity.CreatedAt);
}
