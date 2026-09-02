using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class UpdateAppointmentCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OriginalStatusId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid RequestedStatusId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid VeterinarianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ServiceId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid AvailabilityId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly UpdateAppointmentCommandHandler sut;

    public UpdateAppointmentCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        sut = new UpdateAppointmentCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task STA_T15_Handle_ignores_StatusId_from_request_and_preserves_existing_status()
    {
        var appointment = new Appointment(
            ClientPetId,
            VeterinarianId,
            ServiceId,
            OriginalStatusId,
            AvailabilityId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            "notas");

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var command = new UpdateAppointmentCommand(
            AppointmentId,
            ClientPetId,
            VeterinarianId,
            ServiceId,
            RequestedStatusId,
            AvailabilityId,
            appointment.ScheduledStart.AddHours(1),
            appointment.ScheduledEnd.AddHours(1),
            "actualizado");

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(OriginalStatusId, appointment.StatusId);
        Assert.Equal("actualizado", appointment.Notes);
        await appointmentsRepository.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_appointment_is_missing()
    {
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var command = new UpdateAppointmentCommand(
            AppointmentId,
            ClientPetId,
            VeterinarianId,
            ServiceId,
            RequestedStatusId,
            AvailabilityId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await appointmentsRepository.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}
