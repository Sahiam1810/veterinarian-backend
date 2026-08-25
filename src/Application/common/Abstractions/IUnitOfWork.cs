using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;

namespace Application.Common.Abstractions;

public interface IUnitOfWork
{
    IRolesRepository RolesRepository { get; }

    ISpeciesRepository SpeciesRepository { get; }

    IRaceRepository RacesRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

