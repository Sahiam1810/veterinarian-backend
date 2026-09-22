using Application.Appointments.UseCases;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Appointments;

// Único lugar donde de verdad se rechaza un valor <= 0: ValidationBehavior
// corta acá, antes de que el comando llegue a RecordAppointmentVitalsCommandHandler.
public sealed class RecordAppointmentVitalsCommandValidatorTests
{
    private readonly RecordAppointmentVitalsCommandValidator sut = new();

    [Fact]
    public void Validate_accepts_all_vitals_null()
    {
        var command = CreateCommand(null, null, null, null);

        var result = sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_accepts_positive_values()
    {
        var command = CreateCommand(12.5m, 38.4m, 90, 26);

        var result = sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_non_positive_weight(decimal value)
    {
        var command = CreateCommand(value, 38.4m, 90, 26);

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Weight)
            .WithErrorMessage("El peso debe ser mayor a 0.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_non_positive_temperature(decimal value)
    {
        var command = CreateCommand(12.5m, value, 90, 26);

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Temperature)
            .WithErrorMessage("La temperatura debe ser mayor a 0.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_non_positive_heart_rate(int value)
    {
        var command = CreateCommand(12.5m, 38.4m, value, 26);

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.HeartRate)
            .WithErrorMessage("La frecuencia cardíaca debe ser mayor a 0.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_non_positive_respiratory_rate(int value)
    {
        var command = CreateCommand(12.5m, 38.4m, 90, value);

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RespiratoryRate)
            .WithErrorMessage("La frecuencia respiratoria debe ser mayor a 0.");
    }

    [Fact]
    public void Validate_rejects_empty_appointment_id()
    {
        var command = CreateCommand(12.5m, 38.4m, 90, 26) with { AppointmentId = Guid.Empty };

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AppointmentId)
            .WithErrorMessage("La cita médica es requerida.");
    }

    private static RecordAppointmentVitalsCommand CreateCommand(
        decimal? weight,
        decimal? temperature,
        int? heartRate,
        int? respiratoryRate) =>
        new(Guid.NewGuid(), weight, temperature, heartRate, respiratoryRate);
}
