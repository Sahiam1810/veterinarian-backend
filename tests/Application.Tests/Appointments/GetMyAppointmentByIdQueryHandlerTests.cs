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
using NSubstitute;
using Xunit;
using Application.Tests.Common;

namespace Application.Tests.Appointments;

public sealed class GetMyAppointmentByIdQueryHandlerTests
{
    private Guid ownedClientId;

    private readonly IClientRepository clients = Substitute.For<IClientRepository>();
    private readonly IClientPetRepository clientPets = Substitute.For<IClientPetRepository>();
    private readonly IAppointmentRepository appointments = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetMyAppointmentByIdQueryHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.ClientPetsRepository.Returns(clientPets);
        unitOfWork.AppointmentsRepository.Returns(appointments);
    }

    [Fact]
    public async Task Handle_returns_appointment_owned_by_authenticated_client()
    {
        var (clientPet, appointment) = ArrangeOwnedAppointment();
        var handler = new GetMyAppointmentByIdQueryHandler(unitOfWork);

        var result = await handler.Handle(
            new GetMyAppointmentByIdQuery(appointment.Id, ownedClientId),
            CancellationToken.None);

        Assert.Same(appointment, result);
        Assert.Equal(clientPet.Id, result.ClientPetId);
    }

    [Fact]
    public async Task Handle_hides_appointment_not_owned_by_authenticated_client()
    {
        var (_, appointment) = ArrangeOwnedAppointment();
        clientPets.GetByClientIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClientPetEntity>());
        var handler = new GetMyAppointmentByIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetMyAppointmentByIdQuery(appointment.Id, ownedClientId),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_hides_appointment_of_another_existing_client()
    {
        var (_, appointment) = ArrangeOwnedAppointment();
        var otherClient = TestClients.Create("9876543210", null, phoneNumber: "3009876543");
        clients.GetByIdAsync(otherClient.Id, Arg.Any<CancellationToken>()).Returns(otherClient);
        clientPets.GetByClientIdAsync(otherClient.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClientPetEntity>());
        var handler = new GetMyAppointmentByIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetMyAppointmentByIdQuery(appointment.Id, otherClient.Id),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_not_found_for_an_unknown_client()
    {
        var handler = new GetMyAppointmentByIdQueryHandler(unitOfWork);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetMyAppointmentByIdQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Cliente no encontrado.", exception.Message);
        await appointments.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private (ClientPetEntity ClientPet, Appointment Appointment) ArrangeOwnedAppointment()
    {
        var client = TestClients.Create("1234567890", "Calle 1");
        ownedClientId = client.Id;
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity(
            "Luna",
            4,
            "F",
            12m,
            null,
            species,
            new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);
        var appointment = new Appointment(
            clientPet.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            null);

        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        clientPets.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { clientPet });
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);
        return (clientPet, appointment);
    }
}
