using Domain.Clients.Entities;

namespace Infrastructure.Tests.Support;

// Fábrica de clientes válidos para tests: nombre, correo (único por userId) y
// teléfono ya cumplen las reglas del dominio; cada test sobrescribe solo lo que le importa.
internal static class TestClients
{
    public static ClientEntity Create(
        Guid userId,
        string identificationNumber = "1234567890",
        string? address = null,
        string phoneNumber = "3001234567",
        string? fullName = null,
        string? email = null) =>
        new(
            userId,
            fullName ?? "Cliente de prueba",
            email ?? $"client-{userId:N}@example.test",
            identificationNumber,
            phoneNumber,
            address);
}
