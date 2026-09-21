using Application.AppointmentStatusHistories.Abstraction;
using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Domain.AppointmentStatusHistories.Entities;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using Application.Tests.Common;

namespace Application.Tests.Appointments;

public sealed class CancelMyAppointmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_cancels_an_owned_scheduled_appointment_once()
    {
        var fixture = new Fixture("AGENDADA");

        await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        Assert.Equal(fixture.CancelledStatus.Id, fixture.Appointment.StatusId);
        await fixture.Histories.Received(1).AddAsync(
            Arg.Is<AppointmentStatusHistory>(history =>
                history.AppointmentId == fixture.Appointment.Id
                && history.ClientPetId == fixture.Appointment.ClientPetId
                && history.StatusId == fixture.CancelledStatus.Id),
            Arg.Any<CancellationToken>());
        await fixture.Appointments.Received(1).UpdateAsync(
            fixture.Appointment,
            Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_treats_an_owned_cancelled_appointment_as_idempotent()
    {
        var fixture = new Fixture("CANCELADA");

        await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        await fixture.Histories.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_an_appointment_owned_by_another_client()
    {
        var fixture = new Fixture("AGENDADA");
        fixture.ClientPets.GetByClientIdAsync(
                fixture.Client.Id,
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClientPetEntity>());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_an_owned_appointment_in_another_terminal_status()
    {
        var fixture = new Fixture("ATENDIDA");

        await Assert.ThrowsAsync<ConflictException>(
            () => fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Histories.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    private sealed class Fixture
    {
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public IAppointmentRepository Appointments { get; } =
            Substitute.For<IAppointmentRepository>();
        public IClientPetRepository ClientPets { get; } =
            Substitute.For<IClientPetRepository>();
        public IStatusAppointmentRepository Statuses { get; } =
            Substitute.For<IStatusAppointmentRepository>();
        public IAppointmentStatusHistoryRepository Histories { get; } =
            Substitute.For<IAppointmentStatusHistoryRepository>();

        public UserAccountEntity Account { get; }
        public ClientEntity Client { get; }
        public Appointment Appointment { get; }
        public StatusAppointment CancelledStatus { get; } = new("CANCELADA", null);
        public CancelMyAppointmentCommand Command { get; }
        public CancelMyAppointmentCommandHandler Sut { get; }

        public Fixture(string currentStatusName)
        {
            var userId = Guid.NewGuid();
            Account = new UserAccountEntity(userId, "cliente", "cliente@test.com", "Activo");
            Client = TestClients.Create("1095914051", null);
            var species = new SpeciesEntity("Perro");
            var pet = new PetEntity(
                "Pacho",
                4,
                "M",
                12m,
                null,
                species,
                new RaceEntity("Mestizo", species));
            var clientPet = new ClientPetEntity(Client, pet, true);
            var currentStatus = string.Equals(
                currentStatusName,
                "CANCELADA",
                StringComparison.OrdinalIgnoreCase)
                ? CancelledStatus
                : new StatusAppointment(currentStatusName, null);
            Appointment = new Appointment(
                clientPet.Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                currentStatus.Id,
                Guid.NewGuid(),
                new DateTime(2026, 9, 16, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 16, 14, 30, 0, DateTimeKind.Utc),
                "Control");
            Command = new CancelMyAppointmentCommand(
                Appointment.Id,
                Account.Id,
                "Cancelada desde Telegram");

            UnitOfWork.AppointmentsRepository.Returns(Appointments);
            UnitOfWork.ClientPetsRepository.Returns(ClientPets);
            UnitOfWork.StatusAppointmentsRepository.Returns(Statuses);
            UnitOfWork.AppointmentStatusHistoriesRepository.Returns(Histories);
            UnitOfWork.UserAccountsRepository.GetByIdAsync(
                    Account.Id,
                    Arg.Any<CancellationToken>())
                .Returns(Account);
            UnitOfWork.ClientsRepository.GetByUserIdAsync(
                    Account.UserId,
                    Arg.Any<CancellationToken>())
                .Returns(Client);
            Appointments.GetByIdAsync(Appointment.Id, Arg.Any<CancellationToken>())
                .Returns(Appointment);
            ClientPets.GetByClientIdAsync(Client.Id, Arg.Any<CancellationToken>())
                .Returns(new[] { clientPet });
            Statuses.GetByIdAsync(Appointment.StatusId, Arg.Any<CancellationToken>())
                .Returns(currentStatus);
            Statuses.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(new[] { currentStatus, CancelledStatus });
            Sut = new CancelMyAppointmentCommandHandler(UnitOfWork);
        }
    }
}
