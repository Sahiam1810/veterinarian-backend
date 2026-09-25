using Application.Common.Exceptions;
using Application.HospitalizationStays.Errors;
using Domain.HospitalizationStays.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Xunit;

namespace Infrastructure.Tests.HospitalizationStays;

// Admisiones simultáneas: el índice único funcional UX_HOSP_STAY_ACTIVE_PER_PET rechaza la
// segunda estancia activa con ORA-00001 y UnitOfWork debe devolver 409, no "Data integrity violation".
public sealed class HospitalizationStayActiveUniquenessTests
{
    private const string OracleActiveStayViolation =
        "ORA-00001: unique constraint (VET_APP.UX_HOSP_STAY_ACTIVE_PER_PET) violated";

    [Fact]
    public void Mapper_maps_ora_00001_on_active_stay_index_to_typed_conflict()
    {
        var exception = new DbUpdateException(
            "Save failed",
            new InvalidOperationException(OracleActiveStayViolation));

        var mapped = OracleHospitalizationStayConflictMapper.TryMapToConflict(exception, out var conflict);

        Assert.True(mapped);
        Assert.Equal(HospitalizationStayErrorCodes.ActiveStayAlreadyExists, conflict!.Code);
        Assert.Equal("La mascota ya tiene una estancia activa.", conflict.Message);
    }

    [Theory]
    [InlineData("ORA-00001: unique constraint (VET_APP.UX_CLIENTS_PHONE_NUMBER) violated")]
    [InlineData("ORA-02291: integrity constraint (VET_APP.UX_HOSP_STAY_ACTIVE_PER_PET) violated")]
    public void Mapper_ignores_other_indexes_and_other_oracle_errors(string innerMessage)
    {
        var exception = new DbUpdateException("Save failed", new InvalidOperationException(innerMessage));

        Assert.False(OracleHospitalizationStayConflictMapper.TryMapToConflict(exception, out var conflict));
        Assert.Null(conflict);
    }

    [Fact]
    public async Task UnitOfWork_translates_active_stay_unique_violation_to_409_conflict()
    {
        await using var context = CreateContextFailingWith(OracleActiveStayViolation);
        var unitOfWork = CreateUnitOfWork(context);
        context.Set<HospitalizationStay>().Add(
            new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Segunda admisión"));

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => unitOfWork.SaveChangesAsync());

        Assert.Equal(HospitalizationStayErrorCodes.ActiveStayAlreadyExists, conflict.Code);
    }

    [Fact]
    public async Task UnitOfWork_keeps_unrelated_db_errors_as_DbUpdateException()
    {
        await using var context = CreateContextFailingWith("ORA-02291: integrity constraint violated");
        var unitOfWork = CreateUnitOfWork(context);
        context.Set<HospitalizationStay>().Add(
            new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Admisión"));

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    private static VeterinaryDbContext CreateContextFailingWith(string oracleMessage)
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new FailingSaveInterceptor(oracleMessage))
            .Options;
        return new VeterinaryDbContext(options);
    }

    // UnitOfWork recibe ~36 repositorios; solo importa el DbContext, el resto se sustituye.
    private static UnitOfWork.UnitOfWork CreateUnitOfWork(VeterinaryDbContext context)
    {
        var constructor = typeof(UnitOfWork.UnitOfWork).GetConstructors().Single();
        var arguments = constructor.GetParameters()
            .Select(parameter => parameter.ParameterType == typeof(VeterinaryDbContext)
                ? context
                : Substitute.For([parameter.ParameterType], []))
            .ToArray();
        return (UnitOfWork.UnitOfWork)constructor.Invoke(arguments);
    }

    // Simula la respuesta de Oracle al violar un índice: EF la envuelve en DbUpdateException.
    private sealed class FailingSaveInterceptor(string oracleMessage) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("An error occurred while saving the entity changes.",
                new InvalidOperationException(oracleMessage));
    }
}
