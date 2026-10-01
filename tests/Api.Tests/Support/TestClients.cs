using Domain.Clients.Entities;

namespace Api.Tests.Support;

// Fábrica de clientes válidos para tests: nombre, correo (único por cliente) y
// teléfono ya cumplen las reglas del dominio; cada test sobrescribe solo lo que le importa.
internal static class TestClients
{
    public static ClientEntity Create(
        string identificationNumber = "1234567890",
        string? address = null,
        string phoneNumber = "3001234567",
        string? fullName = null,
        string? email = null) =>
        new(
            fullName ?? "Cliente de prueba",
            email ?? $"client-{Guid.NewGuid():N}@example.test",
            identificationNumber,
            phoneNumber,
            address);
}
