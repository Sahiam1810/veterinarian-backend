using Application.Availabilities.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Diagnostics.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Modules.Abstraction;
using Application.Roles.Abstraction;
using Application.RolePermissions.Abstraction;
using Application.UserPermissions.Abstraction;
using Application.Species.Abstraction;
using Application.StatusAppointments.Abstraction;
using Application.TypeServices.Abstraction;

using Application.Services.Abstraction;

using Application.Specialties.Abstraction;
using Application.ClientsPets.Abstraction;

using Application.Veterinarians.Abstraction;
using Application.Priorities.Abstraction;

using Application.SenderTypes.Abstraction;

using Application.ConversationStatuses.Abstraction;

using Application.MessageTypes.Abstraction;

using Application.EscalationStatuses.Abstraction;

using Application.Notifications.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatEscalationResolutions.Abstraction;
using Application.ChatEscalations.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VeterinaryDbContext _context;

    public UnitOfWork(
        VeterinaryDbContext context,
        IRolesRepository rolesRepository,
        IModulesRepository modulesRepository,
        IRolePermissionsRepository rolePermissionsRepository,
        IUserPermissionsRepository userPermissionsRepository,
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
        IVeterinarianRepository veterinariansRepository,
        IConversationStatusRepository conversationStatusesRepository,
        IMessageTypeRepository messageTypesRepository,
        IPriorityRepository prioritiesRepository,
        IEscalationStatusRepository escalationStatusesRepository,
        IAvailabilityRepository availabilitiesRepository,
        IAppointmentRepository appointmentsRepository,
        IAppointmentStatusHistoryRepository appointmentStatusHistoriesRepository,
        IMedicalRecordRepository medicalRecordsRepository,
        INotificationRepository notificationsRepository,
        IDiagnosticRepository diagnosticsRepository,
        IVaccinationRepository vaccinationsRepository,
        IAgentHumanRepository agentHumansRepository,
        IChatConversationRepository chatConversationsRepository,
        IChatParticipantRepository chatParticipantsRepository,
        IChatMessageRepository chatMessagesRepository,
        IChatEscalationRepository chatEscalationsRepository,
        IChatEscalationResolutionRepository chatEscalationResolutionsRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
        ModulesRepository = modulesRepository;
        RolePermissionsRepository = rolePermissionsRepository;
        UserPermissionsRepository = userPermissionsRepository;
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
        ConversationStatusesRepository = conversationStatusesRepository;
        MessageTypesRepository = messageTypesRepository;
        EscalationStatusesRepository = escalationStatusesRepository;
        AvailabilitiesRepository = availabilitiesRepository;
        AppointmentsRepository = appointmentsRepository;
        AppointmentStatusHistoriesRepository = appointmentStatusHistoriesRepository;
        MedicalRecordsRepository = medicalRecordsRepository;
        NotificationsRepository = notificationsRepository;
        DiagnosticsRepository = diagnosticsRepository;
        VaccinationsRepository = vaccinationsRepository;
        AgentHumansRepository = agentHumansRepository;
        ChatConversationsRepository = chatConversationsRepository;
        ChatParticipantsRepository = chatParticipantsRepository;
        ChatMessagesRepository = chatMessagesRepository;
        ChatEscalationsRepository = chatEscalationsRepository;
        ChatEscalationResolutionsRepository = chatEscalationResolutionsRepository;
    }

    public IRolesRepository RolesRepository { get; }
    public IModulesRepository ModulesRepository { get; }
    public IRolePermissionsRepository RolePermissionsRepository { get; }
    public IUserPermissionsRepository UserPermissionsRepository { get; }
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
    public IConversationStatusRepository ConversationStatusesRepository { get; }
    public IMessageTypeRepository MessageTypesRepository { get; }
    public IEscalationStatusRepository EscalationStatusesRepository { get; }
    public IAvailabilityRepository AvailabilitiesRepository { get; }
    public IAppointmentRepository AppointmentsRepository { get; }
    public IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository { get; }
    public IMedicalRecordRepository MedicalRecordsRepository { get; }
    public IVaccinationRepository VaccinationsRepository { get; }
    public INotificationRepository NotificationsRepository { get; }
    public IDiagnosticRepository DiagnosticsRepository { get; }
    public IAgentHumanRepository AgentHumansRepository { get; }
    public IChatConversationRepository ChatConversationsRepository { get; }
    public IChatParticipantRepository ChatParticipantsRepository { get; }
    public IChatMessageRepository ChatMessagesRepository { get; }
    public IChatEscalationRepository ChatEscalationsRepository { get; }
    public IChatEscalationResolutionRepository ChatEscalationResolutionsRepository { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (OracleClientPhoneConflictMapper.TryMapToConflict(exception, out var conflict)
                  && conflict is not null)
        {
            throw conflict;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational() || _context.Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            cancellationToken);
        try
        {
            await action(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
