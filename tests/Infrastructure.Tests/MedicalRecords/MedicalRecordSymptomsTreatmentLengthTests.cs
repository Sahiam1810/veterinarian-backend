using Domain.MedicalRecords.Entities;
using Infrastructure.MedicalRecords.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.MedicalRecords;

public sealed class MedicalRecordSymptomsTreatmentLengthTests
{
    [Fact]
    public async Task Persists_symptoms_and_treatment_of_200_characters_without_truncating()
    {
        var symptoms = new string('s', 200);
        var treatment = new string('t', 200);

        await using var context = CreateContext();
        var repository = new MedicalRecordRepository(context);
        var record = new MedicalRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            symptoms,
            treatment,
            10m,
            38m);

        await repository.AddAsync(record, CancellationToken.None);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await context.Set<MedicalRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == record.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(symptoms, reloaded!.Symptoms);
        Assert.Equal(treatment, reloaded.Treatment);
        Assert.Equal(200, reloaded.Symptoms!.Length);
        Assert.Equal(200, reloaded.Treatment!.Length);
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }
}
