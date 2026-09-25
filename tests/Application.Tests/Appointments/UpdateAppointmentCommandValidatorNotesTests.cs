using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class UpdateAppointmentCommandValidatorNotesTests
{
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateAppointmentCommandValidator sut;

    public UpdateAppointmentCommandValidatorNotesTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        sut = new UpdateAppointmentCommandValidator(unitOfWork);
    }

    [Fact]
    public async Task Validate_accepts_notes_at_max_length()
    {
        var command = CreateCommand(new string('a', 500));

        var result = await sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public async Task Validate_rejects_notes_over_max_length()
    {
        var command = CreateCommand(new string('a', 501));

        var result = await sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Las notas no pueden exceder 500 caracteres.");
    }

    private static UpdateAppointmentCommand CreateCommand(string? notes)
    {
        var start = DateTime.UtcNow.AddDays(1);
        return new UpdateAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            start.AddMinutes(30),
            notes);
    }
}
