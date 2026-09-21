using Domain.Clients.Entities;
using Infrastructure.Clients.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Clients;

public sealed class ClientRepositoryEmailTests
{
    [Fact]
    public void Client_mapping_has_required_unique_email_index()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ClientEntity));
        Assert.NotNull(entityType);

        var email = entityType!.FindProperty(nameof(ClientEntity.Email));
        Assert.NotNull(email);
        Assert.False(email!.IsNullable);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique
            && index.GetDatabaseName() == "UX_CLIENTS_EMAIL"
            && index.Properties.SequenceEqual(new[] { email }));
    }

    [Fact]
    public async Task GetByEmailAsync_matches_ignoring_case_and_surrounding_spaces()
    {
        await using var context = CreateContext();
        var client = TestClients.Create(email: "ana.perez@test.com");
        context.Set<ClientEntity>().Add(client);
        await context.SaveChangesAsync();
        var repository = new ClientRepository(context);

        var found = await repository.GetByEmailAsync("  Ana.Perez@TEST.com ", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(client.Id, found!.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_returns_null_when_no_client_has_the_email()
    {
        await using var context = CreateContext();
        var repository = new ClientRepository(context);

        var found = await repository.GetByEmailAsync("nadie@test.com", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task ExistsByEmailAsync_detects_an_existing_email()
    {
        await using var context = CreateContext();
        context.Set<ClientEntity>().Add(TestClients.Create(email: "ana@test.com"));
        await context.SaveChangesAsync();
        var repository = new ClientRepository(context);

        Assert.True(await repository.ExistsByEmailAsync("ANA@test.com", CancellationToken.None));
        Assert.False(await repository.ExistsByEmailAsync("otra@test.com", CancellationToken.None));
    }

    [Fact]
    public async Task ExistsByEmailAsync_ignores_the_excluded_client()
    {
        await using var context = CreateContext();
        var client = TestClients.Create(email: "ana@test.com");
        context.Set<ClientEntity>().Add(client);
        await context.SaveChangesAsync();
        var repository = new ClientRepository(context);

        Assert.False(await repository.ExistsByEmailAsync(
            "ana@test.com", CancellationToken.None, excludedId: client.Id));
        Assert.True(await repository.ExistsByEmailAsync(
            "ana@test.com", CancellationToken.None, excludedId: Guid.NewGuid()));
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }
}
