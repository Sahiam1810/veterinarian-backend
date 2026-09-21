using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using Xunit;

namespace Application.Tests.Clients;

public sealed class ClientValueObjectsAndEntityTests
{
    [Fact]
    public void FullName_trims_and_rejects_invalid_values()
    {
        Assert.Equal("Ana Perez", ClientFullName.Create("  Ana Perez ").Value);
        Assert.Throws<ArgumentException>(() => ClientFullName.Create(" "));
        Assert.Throws<ArgumentException>(() => ClientFullName.Create(new string('a', 151)));
    }

    [Fact]
    public void Email_normalizes_and_validates_values()
    {
        Assert.Equal("ana@test.com", ClientEmail.Create(" Ana@Test.COM ").Value);
        Assert.Throws<ArgumentException>(() => ClientEmail.Create("invalid"));
    }

    [Fact]
    public void Client_is_created_active()
    {
        var client = new ClientEntity("Ana", "ana@test.com", "123", "3001234567", null);

        Assert.True(client.IsActive);
    }

    [Fact]
    public void Client_has_no_relationship_with_users()
    {
        // T11: CLIENTS es independiente de USERS; no hay UserId ni navegación a User.
        Assert.Null(typeof(ClientEntity).GetProperty("UserId"));
        Assert.Null(typeof(ClientEntity).GetProperty("User"));
    }

    [Fact]
    public void Client_requires_phone_and_tracks_activation()
    {
        Assert.Throws<ArgumentException>(() => new ClientEntity("Ana", "ana@test.com", "123", "", null));
        var client = new ClientEntity("Ana", "ana@test.com", "123", "3001234567", null);
        client.Deactivate();
        Assert.False(client.IsActive);
        Assert.NotNull(client.UpdatedAt);
        client.Activate();
        Assert.True(client.IsActive);
    }
}
