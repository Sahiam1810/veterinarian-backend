using Application.Common.Abstractions;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Infrastructure.Persistence;

namespace Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VeterinaryDbContext _context;

    public UnitOfWork(
        VeterinaryDbContext context,
        IRolesRepository rolesRepository,
        ISpeciesRepository speciesRepository,
        IRaceRepository racesRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
        SpeciesRepository = speciesRepository;
        RacesRepository = racesRepository;
    }

    public IRolesRepository RolesRepository { get; }

    public ISpeciesRepository SpeciesRepository { get; }

    public IRaceRepository RacesRepository { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
