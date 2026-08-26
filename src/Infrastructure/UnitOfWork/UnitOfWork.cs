using Application.Common.Abstractions;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
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
        IPetRepository petsRepository,
        IUsersRepository usersRepository,
        IUserAccountsRepository userAccountsRepository,
        IUserCredentialsRepository userCredentialsRepository,
        IUserTokensRepository userTokensRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
        SpeciesRepository = speciesRepository;
        RacesRepository = racesRepository;
        PetsRepository = petsRepository;
        UsersRepository = usersRepository;
        UserAccountsRepository = userAccountsRepository;
        UserCredentialsRepository = userCredentialsRepository;
        UserTokensRepository = userTokensRepository;
    }

    public IRolesRepository RolesRepository { get; }
    public ISpeciesRepository SpeciesRepository { get; }
    public IRaceRepository RacesRepository { get; }
    public IPetRepository PetsRepository { get; }
    public IUsersRepository UsersRepository { get; }
    public IUserAccountsRepository UserAccountsRepository { get; }
    public IUserCredentialsRepository UserCredentialsRepository { get; }
    public IUserTokensRepository UserTokensRepository { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}