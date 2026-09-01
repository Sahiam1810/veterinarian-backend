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
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class GetMyAppointmentsQueryHandlerTests
{
    private static readonly Guid UserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetMyAppointmentsQueryHandler sut;

    public GetMyAppointmentsQueryHandlerTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        sut = new GetMyAppointmentsQueryHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_returns_only_appointments_for_client_pets_when_profile_exists()
    {
        var account = new UserAccountEntity(UserId, "cliente", "cliente@test.com", "Active");
        var client = new ClientEntity(UserId, "1234567890", "Calle 1");
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo");
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

        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        clientsRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(client);
        clientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { clientPet });
        appointmentsRepository.GetByClientPetIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == clientPet.Id),
                Arg.Any<CancellationToken>())
            .Returns(expectedAppointments);

        var result = await sut.Handle(new GetMyAppointmentsQuery(UserAccountId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(expectedAppointments[0].Id, result.First().Id);
        await appointmentsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_account_has_no_client_profile()
    {
        var account = new UserAccountEntity(UserId, "vet", "vet@test.com", "Active");

        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        clientsRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyAppointmentsQuery(UserAccountId), CancellationToken.None));

        Assert.Contains("perfil de cliente", exception.Message, StringComparison.OrdinalIgnoreCase);
        await appointmentsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_user_account_does_not_exist()
    {
        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyAppointmentsQuery(UserAccountId), CancellationToken.None));

        await clientsRepository.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
