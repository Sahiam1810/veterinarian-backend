using Application.Clients.Abstraction;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;

using Application.UserTokens.Abstraction;

using Application.StatusAppointments.Abstraction;
using Application.TypeServices.Abstraction;

using Application.Services.Abstraction;

using Application.Specialties.Abstraction;
using Application.ClientsPets.Abstraction;

using Application.Veterinarians.Abstraction;

using Application.SenderTypes.Abstraction;



namespace Application.Common.Abstractions;

public interface IUnitOfWork
{
    IRolesRepository RolesRepository { get; }

    ISpeciesRepository SpeciesRepository { get; }

    IRaceRepository RacesRepository { get; }

    IPetRepository PetsRepository { get; }

    IUsersRepository UsersRepository { get; }

    IStatusAppointmentRepository StatusAppointmentsRepository { get; }

    ITypeServiceRepository TypeServicesRepository { get; }


    IServiceRepository ServicesRepository { get; }

    ISpecialtyRepository SpecialtiesRepository { get; }

    IClientPetRepository ClientPetsRepository { get; }


    IVeterinarianRepository VeterinariansRepository { get; }

    ISenderTypeRepository SenderTypesRepository { get; }



    IUserAccountsRepository UserAccountsRepository { get; }

    IUserCredentialsRepository UserCredentialsRepository { get; }

    IClientRepository ClientsRepository { get; }

    IUserTokensRepository UserTokensRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
