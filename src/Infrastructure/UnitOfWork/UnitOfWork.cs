using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Application.StatusAppointments.Abstraction;
using Application.TypeServices.Abstraction;

using Application.Services.Abstraction;

using Application.Specialties.Abstraction;
using Application.ClientsPets.Abstraction;

using Application.Veterinarians.Abstraction;
using Application.Priorities.Abstraction;

using Application.SenderTypes.Abstraction;

using Application.AiRunStatuses.Abstraction;

using Application.ConversationStatuses.Abstraction;
using Application.EscalationStatuses.Abstraction;



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
        IUsersRepository usersRepository,
        IStatusAppointmentRepository statusAppointmentsRepository,
        ITypeServiceRepository typeServicesRepository,
        IServiceRepository servicesRepository,
        IPetRepository petsRepository,
        IUserAccountsRepository userAccountsRepository,
        IUserCredentialsRepository userCredentialsRepository,

        IClientRepository clientsRepository,

        IUserTokensRepository userTokensRepository,
        ISpecialtyRepository specialtiesRepository,
        IClientPetRepository clientPetsRepository,

        ISenderTypeRepository senderTypesRepository,
        IAiRunStatusRepository aiRunStatusesRepository,


        IVeterinarianRepository veterinariansRepository,

        IConversationStatusRepository conversationStatusesRepository,

        IPriorityRepository prioritiesRepository,
        IEscalationStatusRepository escalationStatusesRepository)


    {
        _context = context;
        RolesRepository = rolesRepository;
        SpeciesRepository = speciesRepository;
        RacesRepository = racesRepository;
        PetsRepository = petsRepository;
        UsersRepository = usersRepository;
        StatusAppointmentsRepository = statusAppointmentsRepository;
        TypeServicesRepository = typeServicesRepository;
        ServicesRepository = servicesRepository;
        UserAccountsRepository = userAccountsRepository;
        UserCredentialsRepository = userCredentialsRepository;

        ClientsRepository = clientsRepository;

        UserTokensRepository = userTokensRepository;
        SpecialtiesRepository = specialtiesRepository;
        ClientPetsRepository = clientPetsRepository;

        VeterinariansRepository = veterinariansRepository;

        PrioritiesRepository = prioritiesRepository;

        SenderTypesRepository = senderTypesRepository;

        AiRunStatusesRepository = aiRunStatusesRepository;

        ConversationStatusesRepository = conversationStatusesRepository;

        EscalationStatusesRepository = escalationStatusesRepository;

    }

    public IRolesRepository RolesRepository { get; }
    public ISpeciesRepository SpeciesRepository { get; }
    public IRaceRepository RacesRepository { get; }
    public IPetRepository PetsRepository { get; }
    public IUsersRepository UsersRepository { get; }
    public IUserAccountsRepository UserAccountsRepository { get; }
    public IUserCredentialsRepository UserCredentialsRepository { get; }

    public IClientRepository ClientsRepository { get; }

    public IUserTokensRepository UserTokensRepository { get; }

    public IStatusAppointmentRepository StatusAppointmentsRepository { get; }
    public ITypeServiceRepository TypeServicesRepository { get; }

    public IServiceRepository ServicesRepository { get; }

    public ISpecialtyRepository SpecialtiesRepository { get; }
    public IClientPetRepository ClientPetsRepository { get; }

    public IVeterinarianRepository VeterinariansRepository { get; }

    public IPriorityRepository PrioritiesRepository { get; }

    public ISenderTypeRepository SenderTypesRepository { get; }
    public IAiRunStatusRepository AiRunStatusesRepository { get; }

    public IConversationStatusRepository ConversationStatusesRepository { get; }

    public IEscalationStatusRepository EscalationStatusesRepository { get; }



    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);


    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await action(cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }
}
