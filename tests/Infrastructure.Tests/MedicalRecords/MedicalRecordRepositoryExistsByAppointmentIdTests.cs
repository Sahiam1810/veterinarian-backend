using Domain.MedicalRecords.Entities;
using Infrastructure.MedicalRecords.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.MedicalRecords;

public sealed class MedicalRecordRepositoryExistsByAppointmentIdTests
{
    [Fact]
    public async Task MR_INFRA_T01_ExistsByAppointmentIdAsync_filters_by_appointment_id()
    {
        var appointmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var otherAppointmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var missingAppointmentId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        await using var context = CreateContext();
        context.Set<MedicalRecord>().Add(new MedicalRecord(
            Guid.NewGuid(),
            appointmentId,
            Guid.NewGuid(),
            "s1",
            "t1",
            10m,
            38m));
        context.Set<MedicalRecord>().Add(new MedicalRecord(
            Guid.NewGuid(),
            otherAppointmentId,
            Guid.NewGuid(),
            "s2",
            "t2",
            11m,
            39m));
        await context.SaveChangesAsync();

        var repository = new MedicalRecordRepository(context);

        Assert.True(await repository.ExistsByAppointmentIdAsync(appointmentId, CancellationToken.None));
        Assert.True(await repository.ExistsByAppointmentIdAsync(otherAppointmentId, CancellationToken.None));
        Assert.False(await repository.ExistsByAppointmentIdAsync(missingAppointmentId, CancellationToken.None));
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }
}
