using Application.Common.Abstractions;

using HelpDesk.Application.Roles.Abstraction;
using Infrastructure.Persistence;

namespace HelpDesk.Infrastructure.UnitOfWork;

using Application.Races.Abstraction;
using Application.Species.Abstraction;
using Infrastructure.Persistence;

namespace Infrastructure.UnitOfWork;


public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VeterinaryDbContext _context;


    public UnitOfWork(VeterinaryDbContext context, IRolesRepository rolesRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
    }

    public IRolesRepository RolesRepository { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    
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

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

}
