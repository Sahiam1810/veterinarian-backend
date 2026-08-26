using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.StatusAppointments.Abstraction;

namespace Application.Common.Abstractions;

public interface IUnitOfWork
{
    IRolesRepository RolesRepository { get; }

    ISpeciesRepository SpeciesRepository { get; }

    IRaceRepository RacesRepository { get; }


    IPetRepository PetsRepository { get; }

    IUsersRepository UsersRepository { get; }

    IStatusAppointmentRepository StatusAppointmentsRepository { get; }

    IUserAccountsRepository UserAccountsRepository { get; }

    IUserCredentialsRepository UserCredentialsRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

