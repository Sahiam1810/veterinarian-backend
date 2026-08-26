using Application.Common.Abstractions;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
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
        IUserAccountsRepository userAccountsRepository,
        IUserCredentialsRepository userCredentialsRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
        SpeciesRepository = speciesRepository;
        RacesRepository = racesRepository;
        UsersRepository = usersRepository;
        UserAccountsRepository = userAccountsRepository;
        UserCredentialsRepository = userCredentialsRepository;
    }

    public IRolesRepository RolesRepository { get; }

    public ISpeciesRepository SpeciesRepository { get; }

    public IRaceRepository RacesRepository { get; }

    public IUsersRepository UsersRepository { get; }

    public IUserAccountsRepository UserAccountsRepository { get; }

    public IUserCredentialsRepository UserCredentialsRepository { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
