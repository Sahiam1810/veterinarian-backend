using Application.Common.Abstractions;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.StatusAppointments.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Persistence;

namespace Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VeterinaryDbContext _context;

    public UnitOfWork(
        VeterinaryDbContext context,
        IRolesRepository rolesRepository,
        ISpeciesRepository speciesRepository,
        IRaceRepository racesRepository,
        IUsersRepository usersRepository,
        IStatusAppointmentRepository statusAppointmentsRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
        SpeciesRepository = speciesRepository;
        RacesRepository = racesRepository;
        UsersRepository = usersRepository;
        StatusAppointmentsRepository = statusAppointmentsRepository;
    }

    public IRolesRepository RolesRepository { get; }

    public ISpeciesRepository SpeciesRepository { get; }

    public IRaceRepository RacesRepository { get; }

    public IUsersRepository UsersRepository { get; }

    public IStatusAppointmentRepository StatusAppointmentsRepository { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
