using Application.Appointments.UseCases;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class CreateMyAppointmentCommandValidatorNotesTests
{
    private readonly CreateMyAppointmentCommandValidator sut = new();

    [Fact]
    public void Validate_accepts_notes_at_max_length()
    {
        var command = CreateCommand(new string('a', 500));

        var result = sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_rejects_notes_over_max_length()
    {
        var command = CreateCommand(new string('a', 501));

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    private static CreateMyAppointmentCommand CreateCommand(string? notes)
    {
        return new CreateMyAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            notes,
            null,
            "idempotency-key-test");
    }
}
