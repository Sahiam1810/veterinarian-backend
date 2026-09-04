using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Infrastructure.Tests.Persistence;

public sealed class AlignmentMigrationTests
{
    [Fact]
    public void Appointment_alignment_migration_is_noop_after_its_schema_migration()
    {
        var migration = new TestableAlignmentMigration();

        var operations = migration.BuildUpOperations();

        Assert.Empty(operations);
    }

    private sealed class TestableAlignmentMigration : AlignAppointmentProfileAndVerificationSchema
    {
        public IReadOnlyList<MigrationOperation> BuildUpOperations()
        {
            var builder = new MigrationBuilder("Oracle.EntityFrameworkCore");
            Up(builder);
            return builder.Operations;
        }
    }
}
