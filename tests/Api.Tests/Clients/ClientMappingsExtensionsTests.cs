using Api.Clients.Mappings;
using Domain.Clients.Entities;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Clients;

public sealed class ClientMappingsExtensionsTests
{
    // S17: el directorio de dueños de Recepcionista mostraba "Cliente Sin Nombre"
    // porque dependía de una segunda llamada a /api/Users (sin permiso para ese
    // rol) para resolver el nombre. El dato ya viene cargado por el
    // .Include(c => c.User) del repositorio -- ToDto() debe exponerlo directo.
    [Fact]
    public void ToDto_populates_full_name_email_and_is_active_from_linked_user()
    {
        var user = new UserEntity("Ana Pérez", "ana.perez@test.com", passwordHash: null, roleId: Guid.NewGuid());
        user.Deactivate();

        var client = new ClientEntity(
            userId: user.Id,
            identificationNumber: "1234567890",
            address: "Calle Falsa 123",
            registrationDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            phoneNumber: "3001234567");
        SetProperty(client, nameof(ClientEntity.User), user);

        var response = client.ToDto();

        Assert.Equal("Ana Pérez", response.FullName);
        Assert.Equal("ana.perez@test.com", response.Email);
        Assert.False(response.IsActive);
    }

    // Si el navigation property no se cargó (no debería pasar dado el
    // .Include(c => c.User) del repositorio), ToDto() no debe reventar: los
    // campos derivados de User caen a null/true en vez de lanzar.
    [Fact]
    public void ToDto_falls_back_to_null_and_active_when_user_navigation_is_missing()
    {
        var client = new ClientEntity(
            userId: Guid.NewGuid(),
            identificationNumber: "9876543210",
            address: null,
            registrationDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            phoneNumber: null);

        var response = client.ToDto();

        Assert.Null(response.FullName);
        Assert.Null(response.Email);
        Assert.True(response.IsActive);
    }

    private static void SetProperty(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

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
