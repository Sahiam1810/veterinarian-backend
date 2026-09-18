using Application.Clients.Errors;
using Domain.Clients.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Clients;

public sealed class ClientPhoneUniquenessPersistenceTests
{
    [Fact]
    public void Client_mapping_has_nullable_unique_phone_number_index()
    {
        using var context = CreateOracleModelContext();
        var entityType = context.Model.FindEntityType(typeof(ClientEntity));
        Assert.NotNull(entityType);

        var phone = entityType!.FindProperty(nameof(ClientEntity.PhoneNumber));
        Assert.NotNull(phone);
        Assert.True(phone!.IsNullable);

        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique
            && index.GetDatabaseName() == "UX_CLIENTS_PHONE_NUMBER"
            && index.Properties.SequenceEqual(new[] { phone }));
    }

    [Fact]
    public void Mapper_maps_ora_00001_on_phone_index_to_typed_conflict()
    {
        var inner = new InvalidOperationException(
            "ORA-00001: unique constraint (VET_APP.UX_CLIENTS_PHONE_NUMBER) violated");
        var exception = new DbUpdateException("Save failed", inner);

        var mapped = OracleClientPhoneConflictMapper.TryMapToConflict(exception, out var conflict);

        Assert.True(mapped);
        Assert.NotNull(conflict);
        Assert.Equal(ClientErrorCodes.PhoneAlreadyInUse, conflict!.Code);
    }

    [Fact]
    public void Mapper_ignores_ora_00001_on_unrelated_indexes()
    {
        var inner = new InvalidOperationException(
            "ORA-00001: unique constraint (VET_APP.IX_CLIENTS_IDENTIFICATION_NUMBER) violated");
        var exception = new DbUpdateException("Save failed", inner);

        var mapped = OracleClientPhoneConflictMapper.TryMapToConflict(exception, out var conflict);

        Assert.False(mapped);
        Assert.Null(conflict);
    }

    private static VeterinaryDbContext CreateOracleModelContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=unused;Password=unused;Data Source=unused")
            .Options;
        return new VeterinaryDbContext(options);
    }
}
