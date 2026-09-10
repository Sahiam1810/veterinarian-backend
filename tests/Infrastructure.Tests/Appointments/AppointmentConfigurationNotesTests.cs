using Domain.Appointments.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Appointments;

public sealed class AppointmentConfigurationNotesTests
{
    [Fact]
    public void Notes_is_mapped_to_500_characters()
    {
        using var context = CreateOracleModelContext();
        var entityType = context.Model.FindEntityType(typeof(Appointment));
        Assert.NotNull(entityType);

        var notes = entityType!.FindProperty(nameof(Appointment.Notes));

        Assert.NotNull(notes);
        Assert.Equal(500, notes!.GetMaxLength());
        Assert.Equal("VARCHAR2(500)", notes.GetColumnType());
    }

    private static VeterinaryDbContext CreateOracleModelContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=unused;Password=unused;Data Source=unused")
            .Options;
        return new VeterinaryDbContext(options);
    }
}
