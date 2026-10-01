using Domain.MedicationOrders.Entities;
using Domain.ProcedureOrders.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Infrastructure.Tests.MedicationOrders;

// Una orden médica nace de una cita o de una estancia: la base lo garantiza con un CHECK.
public sealed class MedicalOrderOriginPersistenceModelTests
{
    [Theory]
    [InlineData(typeof(MedicationOrder), "CK_MEDICATION_ORDERS_ORIGIN")]
    [InlineData(typeof(ProcedureOrder), "CK_PROCEDURE_ORDERS_ORIGIN")]
    public void Order_mapping_allows_null_appointment_and_enforces_single_origin(Type orderType, string checkName)
    {
        using var context = CreateOracleModelContext();
        // Los CHECK solo viven en el modelo de diseño (el que usan las migraciones).
        var entityType = context.GetService<IDesignTimeModel>().Model.FindEntityType(orderType);
        Assert.NotNull(entityType);

        Assert.True(entityType!.FindProperty("AppointmentId")!.IsNullable);
        Assert.True(entityType.FindProperty("HospitalizationStayId")!.IsNullable);

        var check = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == checkName);
        Assert.Contains("APPOINTMENT_ID IS NOT NULL AND HOSPITALIZATION_STAY_ID IS NULL", check.Sql);
        Assert.Contains("APPOINTMENT_ID IS NULL AND HOSPITALIZATION_STAY_ID IS NOT NULL", check.Sql);
    }

    [Theory]
    [InlineData(typeof(MedicationOrder))]
    [InlineData(typeof(ProcedureOrder))]
    public void Appointment_foreign_key_keeps_cascade_delete(Type orderType)
    {
        using var context = CreateOracleModelContext();
        var foreignKey = Assert.Single(
            context.Model.FindEntityType(orderType)!.GetForeignKeys(),
            fk => fk.Properties.Single().Name == "AppointmentId");

        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    private static VeterinaryDbContext CreateOracleModelContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=unused;Password=unused;Data Source=unused")
            .Options;
        return new VeterinaryDbContext(options);
    }
}
