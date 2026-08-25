using Application.Common.Abstractions;
using Application.Races.Abstraction;
using Application.Species.Abstraction;
using Infrastructure.Persistence;

namespace Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VeterinaryDbContext _context;

    public ISpeciesRepository SpeciesRepository { get; }
    public IRaceRepository RacesRepository { get; }

    public UnitOfWork(
        VeterinaryDbContext context,
        ISpeciesRepository speciesRepository,
        IRaceRepository racesRepository)
    {
        _context = context;
        SpeciesRepository = speciesRepository;
        RacesRepository = racesRepository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
