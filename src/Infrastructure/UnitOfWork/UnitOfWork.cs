using Application.AccountStatements.Abstraction;
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
using Application.Roles.Abstraction;
using Application.RolePermissions.Abstraction;
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

using Application.MessageTypes.Abstraction;

using Application.EscalationStatuses.Abstraction;




using Application.Notifications.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.AiModels.Abstraction;
using Application.ChatConversationAssignments.Abstraction;
using Application.ChatConversationAiSettings.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatAiRunErrors.Abstraction;
using Application.ChatAiRunMetrics.Abstraction;
using Application.ChatAiRuns.Abstraction;
using Application.ChatAttachments.Abstraction;
using Application.ChatEscalationAssignments.Abstraction;
using Application.ChatEscalationResolutions.Abstraction;
using Application.ChatEscalations.Abstraction;
using Application.ChatEscalationStatusHistories.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.ChatUserProfiles.Abstraction;
using Application.ProviderModelsAi.Abstraction;
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
        IRolePermissionsRepository rolePermissionsRepository,
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

        IAccountStatementsRepository accountStatementsRepository,

        ISenderTypeRepository senderTypesRepository,
        IAiRunStatusRepository aiRunStatusesRepository,


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
        IAiModelRepository aiModelsRepository,
        IChatUserProfileRepository chatUserProfilesRepository,
        IChatConversationRepository chatConversationsRepository,
        IChatConversationAssignmentRepository chatConversationAssignmentsRepository,
        IChatConversationAiSettingRepository chatConversationAiSettingsRepository,
        IChatParticipantRepository chatParticipantsRepository,
        IChatMessageRepository chatMessagesRepository,
        IChatAttachmentRepository chatAttachmentsRepository,
        IChatEscalationRepository chatEscalationsRepository,
        IChatEscalationStatusHistoryRepository chatEscalationStatusHistoriesRepository,
        IChatEscalationResolutionRepository chatEscalationResolutionsRepository,
        IChatEscalationAssignmentRepository chatEscalationAssignmentsRepository,
        IChatAiRunRepository chatAiRunsRepository,
        IChatAiRunMetricsRepository chatAiRunMetricsRepository,
        IChatAiRunErrorRepository chatAiRunErrorsRepository,
        IProviderModelAiRepository providerModelsAiRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
        RolePermissionsRepository = rolePermissionsRepository;
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

        AccountStatementsRepository = accountStatementsRepository;


        VeterinariansRepository = veterinariansRepository;

        PrioritiesRepository = prioritiesRepository;

        SenderTypesRepository = senderTypesRepository;

        AiRunStatusesRepository = aiRunStatusesRepository;

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
        AiModelsRepository = aiModelsRepository;
        ChatUserProfilesRepository = chatUserProfilesRepository;
        ChatConversationsRepository = chatConversationsRepository;
        ChatConversationAssignmentsRepository = chatConversationAssignmentsRepository;
        ChatConversationAiSettingsRepository = chatConversationAiSettingsRepository;
        ChatParticipantsRepository = chatParticipantsRepository;
        ChatMessagesRepository = chatMessagesRepository;
        ChatAttachmentsRepository = chatAttachmentsRepository;
        ChatEscalationsRepository = chatEscalationsRepository;
        ChatEscalationStatusHistoriesRepository = chatEscalationStatusHistoriesRepository;
        ChatEscalationResolutionsRepository = chatEscalationResolutionsRepository;
        ChatEscalationAssignmentsRepository = chatEscalationAssignmentsRepository;
        ChatAiRunsRepository = chatAiRunsRepository;
        ChatAiRunMetricsRepository = chatAiRunMetricsRepository;
        ChatAiRunErrorsRepository = chatAiRunErrorsRepository;
        ProviderModelsAiRepository = providerModelsAiRepository;
    }

    public IRolesRepository RolesRepository { get; }
    public IRolePermissionsRepository RolePermissionsRepository { get; }
    public ISpeciesRepository SpeciesRepository { get; }
    public IRaceRepository RacesRepository { get; }
    public IPetRepository PetsRepository { get; }
    public IUsersRepository UsersRepository { get; }
    public IUserAccountsRepository UserAccountsRepository { get; }
    public IUserCredentialsRepository UserCredentialsRepository { get; }

    public IClientRepository ClientsRepository { get; }

    public IUserTokensRepository UserTokensRepository { get; }

    public IAccountStatementsRepository AccountStatementsRepository { get; }

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
    public IAiModelRepository AiModelsRepository { get; }
    public IChatUserProfileRepository ChatUserProfilesRepository { get; }
    public IChatConversationRepository ChatConversationsRepository { get; }
    public IChatConversationAssignmentRepository ChatConversationAssignmentsRepository { get; }
    public IChatConversationAiSettingRepository ChatConversationAiSettingsRepository { get; }
    public IChatParticipantRepository ChatParticipantsRepository { get; }
    public IChatMessageRepository ChatMessagesRepository { get; }
    public IChatAttachmentRepository ChatAttachmentsRepository { get; }
    public IChatEscalationRepository ChatEscalationsRepository { get; }
    public IChatEscalationStatusHistoryRepository ChatEscalationStatusHistoriesRepository { get; }
    public IChatEscalationResolutionRepository ChatEscalationResolutionsRepository { get; }
    public IChatEscalationAssignmentRepository ChatEscalationAssignmentsRepository { get; }
    public IChatAiRunRepository ChatAiRunsRepository { get; }
    public IChatAiRunMetricsRepository ChatAiRunMetricsRepository { get; }
    public IChatAiRunErrorRepository ChatAiRunErrorsRepository { get; }
    public IProviderModelAiRepository ProviderModelsAiRepository { get; }

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
