using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using NSubstitute;
using Xunit;
using Application.Tests.Common;

namespace Application.Tests.Appointments;

public sealed class GetMyAppointmentsQueryHandlerTests
{
    private static readonly Guid UnknownClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 15, 0, 0, TimeSpan.Zero);

    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetMyAppointmentsQueryHandler sut;
    private Guid ownedClientId;

    public GetMyAppointmentsQueryHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        sut = new GetMyAppointmentsQueryHandler(unitOfWork, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Handle_SEC_01_T01_client_with_profile_returns_only_own_appointments()
    {
        var client = TestClients.Create("1234567890", "Calle 1");
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        var pet = new PetEntity("Firulais", 3, "M", 10m, null, species, race);
        var clientPet = new ClientPetEntity(client, pet, true);
        var expectedAppointments = new[]
        {
            new Appointment(
                clientPet.Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null)
        };

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        clientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { clientPet });
        appointmentsRepository.GetByClientPetIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == clientPet.Id),
                Arg.Any<CancellationToken>())
            .Returns(expectedAppointments);

        var result = await sut.Handle(new GetMyAppointmentsQuery(client.Id), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(expectedAppointments[0].Id, result.First().Id);
        await appointmentsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SEC_01_T02_unknown_client_throws_not_found()
    {
        clientsRepository.GetByIdAsync(UnknownClientId, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyAppointmentsQuery(UnknownClientId), CancellationToken.None));

        Assert.Equal("Cliente no encontrado.", exception.Message);
    }

    [Fact]
    public async Task Handle_SEC_01_T03_unknown_client_does_not_read_any_appointments()
    {
        clientsRepository.GetByIdAsync(UnknownClientId, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyAppointmentsQuery(UnknownClientId), CancellationToken.None));

        await appointmentsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await appointmentsRepository.DidNotReceive()
            .GetByClientPetIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await clientPetsRepository.DidNotReceive()
            .GetByClientIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SEC_01_T04_client_without_pets_does_not_receive_other_client_appointments()
    {
        var client = TestClients.Create("1122334455", "Calle 3", phoneNumber: "3001112233");
        var otherClient = TestClients.Create("0987654321", "Calle 2");
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        var pet = new PetEntity("Otro", 2, "F", 8m, null, species, race);
        var otherClientPet = new ClientPetEntity(otherClient, pet, true);
        var otherClientAppointments = new[]
        {
            new Appointment(
                otherClientPet.Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null)
        };

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        clientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClientPetEntity>());
        appointmentsRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(otherClientAppointments);

        var result = await sut.Handle(new GetMyAppointmentsQuery(client.Id), CancellationToken.None);

        Assert.Empty(result);
        await appointmentsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SEC_01_T05_propagates_cancellation_token()
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var client = TestClients.Create("1234567890", "Calle 1");
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        var pet = new PetEntity("Firulais", 3, "M", 10m, null, species, race);
        var clientPet = new ClientPetEntity(client, pet, true);
        var expectedAppointments = new[]
        {
            new Appointment(
                clientPet.Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null)
        };

        clientsRepository.GetByIdAsync(client.Id, cancellationToken)
            .Returns(client);
        clientPetsRepository.GetByClientIdAsync(client.Id, cancellationToken)
            .Returns(new[] { clientPet });
        appointmentsRepository.GetByClientPetIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == clientPet.Id),
                cancellationToken)
            .Returns(expectedAppointments);

        var result = await sut.Handle(new GetMyAppointmentsQuery(client.Id), cancellationToken);

        Assert.Single(result);
        await clientsRepository.Received(1).GetByIdAsync(client.Id, cancellationToken);
        await clientPetsRepository.Received(1).GetByClientIdAsync(client.Id, cancellationToken);
        await appointmentsRepository.Received(1).GetByClientPetIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == clientPet.Id),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_upcoming_returns_only_future_scheduled_appointments()
    {
        var appointments = ArrangeOwnedAppointments(
            CreateAppointment("AGENDADA", Now.AddHours(1), Now.AddHours(2)),
            CreateAppointment("AGENDADA", Now.AddHours(-2), Now.AddHours(-1)),
            CreateAppointment("CANCELADA", Now.AddHours(3), Now.AddHours(4)));

        var result = await sut.Handle(
            new GetMyAppointmentsQuery(ownedClientId, AppointmentQueryScope.Upcoming),
            CancellationToken.None);

        var appointment = Assert.Single(result);
        Assert.Equal(appointments[0].Id, appointment.Id);
    }

    [Fact]
    public async Task Handle_history_returns_finished_or_non_scheduled_appointments()
    {
        var appointments = ArrangeOwnedAppointments(
            CreateAppointment("AGENDADA", Now.AddHours(1), Now.AddHours(2)),
            CreateAppointment("AGENDADA", Now.AddHours(-2), Now.AddHours(-1)),
            CreateAppointment("ATENDIDA", Now.AddHours(-3), Now.AddHours(-2)));

        var result = await sut.Handle(
            new GetMyAppointmentsQuery(ownedClientId, AppointmentQueryScope.History),
            CancellationToken.None);

        Assert.Equal(new[] { appointments[1].Id, appointments[2].Id }, result.Select(x => x.Id));
    }

    private Appointment[] ArrangeOwnedAppointments(params Appointment[] appointments)
    {
        var client = TestClients.Create("1234567890", "Calle 1");
        ownedClientId = client.Id;
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity(
            "Firulais",
            3,
            "M",
            10m,
            null,
            species,
            new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);
        clientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { clientPet });
        appointmentsRepository.GetByClientPetIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(appointments);
        return appointments;
    }

    private static Appointment CreateAppointment(
        string statusName,
        DateTimeOffset scheduledStart,
        DateTimeOffset scheduledEnd)
    {
        var status = new StatusAppointment(statusName, null);
        var appointment = new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status.Id,
            Guid.NewGuid(),
            scheduledStart.UtcDateTime,
            scheduledEnd.UtcDateTime,
            null);
        typeof(Appointment).GetProperty(nameof(Appointment.Status))!.SetValue(appointment, status);
        return appointment;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
