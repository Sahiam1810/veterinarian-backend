using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.UserAccounts.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Appointments;

public sealed class GetMyAppointmentByIdQueryHandlerTests
{
    private static readonly Guid UserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly IUserAccountsRepository userAccounts = Substitute.For<IUserAccountsRepository>();
    private readonly IClientRepository clients = Substitute.For<IClientRepository>();
    private readonly IClientPetRepository clientPets = Substitute.For<IClientPetRepository>();
    private readonly IAppointmentRepository appointments = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetMyAppointmentByIdQueryHandlerTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccounts);
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
            new GetMyAppointmentByIdQuery(appointment.Id, UserAccountId),
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
            new GetMyAppointmentByIdQuery(appointment.Id, UserAccountId),
            CancellationToken.None));
    }

    private (ClientPetEntity ClientPet, Appointment Appointment) ArrangeOwnedAppointment()
    {
        var account = new UserAccountEntity(UserId, "cliente", "cliente@test.com", "Active");
        var client = new ClientEntity(UserId, "1234567890", "Calle 1");
        var pet = new PetEntity(
            "Luna",
            4,
            "F",
            12m,
            null,
            new SpeciesEntity("Canino"),
            new RaceEntity("Mestizo"));
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

        userAccounts.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>()).Returns(account);
        clients.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(client);
        clientPets.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { clientPet });
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);
        return (clientPet, appointment);
    }
}
