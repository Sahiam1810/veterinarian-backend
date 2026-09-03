using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Specialties.Entities;
using Domain.Species.Entities;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Appointments;

public sealed class GetAppointmentBookingOptionsQueryTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_returns_only_owned_pets_active_services_and_active_veterinarians()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new UserAccountEntity(userId, "cliente", "cliente@test.com", "Activo");
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: null);
        var pet = new PetEntity(
            "Luna", 4, "F", 12m, null, new SpeciesEntity("Canino"), new RaceEntity("Mestizo"));
        var clientPet = new ClientPetEntity(client, pet, true);
        var activeService = new Service(Guid.NewGuid(), "Consulta", 30, 50000m);
        var inactiveService = new Service(Guid.NewGuid(), "Baño", 60, 40000m, false);
        var veterinarian = CreateVeterinarian("Dra. Ana", true);
        var inactiveVeterinarian = CreateVeterinarian("Dr. Inactivo", false);
        Configure(accountId, account, client, clientPet, pet, activeService, inactiveService,
            veterinarian, inactiveVeterinarian);

        var result = await new GetAppointmentBookingOptionsQueryHandler(unitOfWork)
            .Handle(new GetAppointmentBookingOptionsQuery(accountId), CancellationToken.None);

        Assert.Equal("Luna", Assert.Single(result.Pets).Name);
        Assert.Equal("Consulta", Assert.Single(result.Services).Name);
        Assert.Equal("Dra. Ana", Assert.Single(result.Veterinarians).FullName);
        Assert.True(result.RequiresRequesterPhoneNumber);
    }

    [Fact]
    public async Task Handle_without_client_profile_is_not_found()
    {
        var accountId = Guid.NewGuid();
        var account = new UserAccountEntity(Guid.NewGuid(), "cliente", "cliente@test.com", "Activo");
        unitOfWork.UserAccountsRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);
        unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var action = () => new GetAppointmentBookingOptionsQueryHandler(unitOfWork)
            .Handle(new GetAppointmentBookingOptionsQuery(accountId), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    private void Configure(
        Guid accountId, UserAccountEntity account, ClientEntity client, ClientPetEntity clientPet,
        PetEntity pet, Service activeService, Service inactiveService,
        Veterinarian veterinarian, Veterinarian inactiveVeterinarian)
    {
        unitOfWork.UserAccountsRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);
        unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>())
            .Returns(client);
        unitOfWork.ClientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { clientPet });
        unitOfWork.PetsRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { pet });
        unitOfWork.ServicesRepository.GetAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { activeService, inactiveService });
        unitOfWork.VeterinariansRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { veterinarian, inactiveVeterinarian });
    }

    private static Veterinarian CreateVeterinarian(string name, bool active)
    {
        var user = new UserEntity(name, $"{Guid.NewGuid():N}@test.com", "hash", Guid.NewGuid());
        if (!active)
        {
            user.Deactivate();
        }
        var specialty = new SpecialtyEntity("General", null);
        var veterinarian = new Veterinarian(user.Id, specialty.Id, Guid.NewGuid().ToString("N"));
        typeof(Veterinarian).GetProperty(nameof(Veterinarian.User))!.SetValue(veterinarian, user);
        typeof(Veterinarian).GetProperty(nameof(Veterinarian.Specialty))!
            .SetValue(veterinarian, specialty);
        return veterinarian;
    }
}
