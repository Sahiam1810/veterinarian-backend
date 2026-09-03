using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class CreateAppointmentConcurrencyTests
{
    [Fact]
    public async Task Handle_locks_availability_and_rechecks_overlap_before_insert()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var availabilities = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
        var veterinarianId = Guid.NewGuid();
        var availability = new Availability(
            veterinarianId, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(18, 0));
        var command = new CreateAppointmentCommand(
            Guid.NewGuid(), veterinarianId, Guid.NewGuid(), Guid.NewGuid(), availability.Id,
            new DateTime(2026, 9, 7, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 7, 14, 30, 0, DateTimeKind.Utc),
            null, "3001234567");

        unitOfWork.AppointmentsRepository.Returns(appointments);
        unitOfWork.AvailabilitiesRepository.Returns(availabilities);
        availabilities.LockByIdAsync(availability.Id, Arg.Any<CancellationToken>())
            .Returns(availability);
        appointments.HasOverlappingAppointmentAsync(
                command.ClientPetId, veterinarianId, command.ScheduledStart, command.ScheduledEnd,
                null, Arg.Any<CancellationToken>())
            .Returns(true);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                call.ArgAt<CancellationToken>(1)));

        var handler = new CreateAppointmentCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
        await availabilities.Received(1)
            .LockByIdAsync(availability.Id, Arg.Any<CancellationToken>());
        await appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}
