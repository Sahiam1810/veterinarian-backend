using Domain.MedicalRecords.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.MedicalRecords;

public sealed class MedicalRecordConfigurationTests
{
    [Fact]
    public void Symptoms_and_treatment_are_mapped_to_1000_characters()
    {
        using var context = CreateOracleModelContext();
        var entityType = context.Model.FindEntityType(typeof(MedicalRecord));
        Assert.NotNull(entityType);

        var symptoms = entityType!.FindProperty(nameof(MedicalRecord.Symptoms));
        var treatment = entityType.FindProperty(nameof(MedicalRecord.Treatment));

        Assert.NotNull(symptoms);
        Assert.NotNull(treatment);
        Assert.Equal(1000, symptoms!.GetMaxLength());
        Assert.Equal(1000, treatment!.GetMaxLength());
        Assert.Equal("VARCHAR2(1000)", symptoms.GetColumnType());
        Assert.Equal("VARCHAR2(1000)", treatment.GetColumnType());
    }

    private static VeterinaryDbContext CreateOracleModelContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=unused;Password=unused;Data Source=unused")
            .Options;
        return new VeterinaryDbContext(options);
    }
}
