
using HelpDesk.Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.Races.Abstraction;


namespace Application.Common.Abstractions;

public interface IUnitOfWork
{

    IRolesRepository RolesRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

    ISpeciesRepository SpeciesRepository { get; }
    IRaceRepository RacesRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

