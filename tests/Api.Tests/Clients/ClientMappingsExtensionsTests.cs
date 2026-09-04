using Api.Clients.Mappings;
using Domain.Clients.Entities;
using Xunit;

namespace Api.Tests.Clients;

public sealed class ClientMappingsExtensionsTests
{
    // El lookup anónimo por cédula (GET /api/clients/by-identification/{id}) no
    // exige JWT -- cualquiera que conozca un número de identificación válido
    // puede llamarlo. Este test fija que esa respuesta nunca vuelva a incluir
    // Address/PhoneNumber, aunque alguien extienda el DTO en el futuro.
    [Fact]
    public void ToIdentificationLookupResponse_never_exposes_address_or_phone_number()
    {
        var client = new ClientEntity(
            userId: Guid.NewGuid(),
            identificationNumber: "1234567890",
            address: "Calle Falsa 123",
            registrationDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            phoneNumber: "3001234567");

        var response = client.ToIdentificationLookupResponse();

        Assert.Equal(client.Id, response.Id);
        Assert.Equal(client.UserId, response.UserId);
        Assert.Equal("1234567890", response.IdentificationNumber);
        Assert.Equal(client.RegistrationDate, response.RegistrationDate);

        var responseProperties = response.GetType().GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("Address", responseProperties);
        Assert.DoesNotContain("PhoneNumber", responseProperties);
    }

    // Tarea 2.2: mismo recorte de PII que by-identification.
    [Fact]
    public void ToPhoneLookupResponse_never_exposes_address_or_phone_number()
    {
        var client = new ClientEntity(
            userId: Guid.NewGuid(),
            identificationNumber: "1234567890",
            address: "Calle Falsa 123",
            registrationDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            phoneNumber: "3001234567");

        var response = client.ToPhoneLookupResponse();

        Assert.Equal(client.Id, response.Id);
        Assert.Equal(client.UserId, response.UserId);
        Assert.Equal("1234567890", response.IdentificationNumber);
        Assert.Equal(client.RegistrationDate, response.RegistrationDate);

        var responseProperties = response.GetType().GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("Address", responseProperties);
        Assert.DoesNotContain("PhoneNumber", responseProperties);
    }
}
