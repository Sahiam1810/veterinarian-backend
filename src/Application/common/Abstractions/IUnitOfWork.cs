using Application.Species.Abstraction;
using Application.Races.Abstraction;

namespace Application.Common.Abstractions;

public interface IUnitOfWork
{
    ISpeciesRepository SpeciesRepository { get; }
    IRaceRepository RacesRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}