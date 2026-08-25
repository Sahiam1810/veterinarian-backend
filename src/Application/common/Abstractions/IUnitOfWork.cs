using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.Users.Abstraction;

namespace Application.Common.Abstractions;

public interface IUnitOfWork
{
    IRolesRepository RolesRepository { get; }

    ISpeciesRepository SpeciesRepository { get; }

    IRaceRepository RacesRepository { get; }

    IUsersRepository UsersRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

