using Application.Availabilities.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Diagnostics.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Clients.Abstraction;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Modules.Abstraction;
using Application.Roles.Abstraction;
using Application.RolePermissions.Abstraction;
using Application.Species.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;

using Application.UserTokens.Abstraction;

using Application.StatusAppointments.Abstraction;
using Application.TypeServices.Abstraction;

using Application.Services.Abstraction;

using Application.Specialties.Abstraction;
using Application.ClientsPets.Abstraction;

using Application.Veterinarians.Abstraction;
using Application.Priorities.Abstraction;

using Application.SenderTypes.Abstraction;

using Application.AccountStatements.Abstraction;

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

namespace Application.Common.Abstractions;

public interface IUnitOfWork
{
    IRolesRepository RolesRepository { get; }

    IModulesRepository ModulesRepository { get; }

    IRolePermissionsRepository RolePermissionsRepository { get; }

    IModuleRepository ModulesRepository { get; }

    ISpeciesRepository SpeciesRepository { get; }

    IRaceRepository RacesRepository { get; }

    IPetRepository PetsRepository { get; }

    IUsersRepository UsersRepository { get; }

    IStatusAppointmentRepository StatusAppointmentsRepository { get; }

    ITypeServiceRepository TypeServicesRepository { get; }


    IServiceRepository ServicesRepository { get; }

    ISpecialtyRepository SpecialtiesRepository { get; }

    IClientPetRepository ClientPetsRepository { get; }


    IVeterinarianRepository VeterinariansRepository { get; }

    IPriorityRepository PrioritiesRepository { get; }

    ISenderTypeRepository SenderTypesRepository { get; }


    IAiRunStatusRepository AiRunStatusesRepository { get; }

    IConversationStatusRepository ConversationStatusesRepository { get; }


    IMessageTypeRepository MessageTypesRepository { get; }

    IEscalationStatusRepository EscalationStatusesRepository { get; }

    IAppointmentRepository AppointmentsRepository { get; }

    IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository { get; }

    IMedicalRecordRepository MedicalRecordsRepository { get; }

    IVaccinationRepository VaccinationsRepository { get; }

    INotificationRepository NotificationsRepository { get; }

    IDiagnosticRepository DiagnosticsRepository { get; }

    IAgentHumanRepository AgentHumansRepository { get; }

    IAiModelRepository AiModelsRepository { get; }

    IChatUserProfileRepository ChatUserProfilesRepository { get; }

    IChatConversationRepository ChatConversationsRepository { get; }

    IChatConversationAssignmentRepository ChatConversationAssignmentsRepository { get; }

    IChatConversationAiSettingRepository ChatConversationAiSettingsRepository { get; }

    IChatParticipantRepository ChatParticipantsRepository { get; }

    IChatMessageRepository ChatMessagesRepository { get; }

    IChatAttachmentRepository ChatAttachmentsRepository { get; }

    IChatEscalationRepository ChatEscalationsRepository { get; }

    IChatEscalationStatusHistoryRepository ChatEscalationStatusHistoriesRepository { get; }

    IChatEscalationResolutionRepository ChatEscalationResolutionsRepository { get; }

    IChatEscalationAssignmentRepository ChatEscalationAssignmentsRepository { get; }

    IChatAiRunRepository ChatAiRunsRepository { get; }

    IChatAiRunMetricsRepository ChatAiRunMetricsRepository { get; }

    IChatAiRunErrorRepository ChatAiRunErrorsRepository { get; }

    IProviderModelAiRepository ProviderModelsAiRepository { get; }

    IUserAccountsRepository UserAccountsRepository { get; }

    IUserCredentialsRepository UserCredentialsRepository { get; }

    IClientRepository ClientsRepository { get; }

    IUserTokensRepository UserTokensRepository { get; }

    IAccountStatementsRepository AccountStatementsRepository { get; }

    IAvailabilityRepository AvailabilitiesRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
