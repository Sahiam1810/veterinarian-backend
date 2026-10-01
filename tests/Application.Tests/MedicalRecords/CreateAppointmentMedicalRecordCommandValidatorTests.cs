using Application.MedicalRecords.UseCases;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.MedicalRecords;

public sealed class CreateAppointmentMedicalRecordCommandValidatorTests
{
    private readonly CreateAppointmentMedicalRecordCommandValidator sut = new();

    [Fact]
    public void Validate_accepts_symptoms_and_treatment_at_max_length()
    {
        var command = CreateCommand(new string('a', 1000), new string('b', 1000));

        var result = sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Symptoms);
        result.ShouldNotHaveValidationErrorFor(x => x.Treatment);
    }

    [Fact]
    public void Validate_rejects_symptoms_over_max_length()
    {
        var command = CreateCommand(new string('a', 1001), "Tratamiento");

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Symptoms)
            .WithErrorMessage("Los síntomas no pueden exceder 1000 caracteres.");
    }

    [Fact]
    public void Validate_rejects_treatment_over_max_length()
    {
        var command = CreateCommand("Síntomas", new string('b', 1001));

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Treatment)
            .WithErrorMessage("El tratamiento no puede exceder 1000 caracteres.");
    }

    private static CreateAppointmentMedicalRecordCommand CreateCommand(string? symptoms, string? treatment)
    {
        return new CreateAppointmentMedicalRecordCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            symptoms,
            treatment,
            10m,
            38.5m,
            null,
            Guid.NewGuid(),
            false);
    }
}
