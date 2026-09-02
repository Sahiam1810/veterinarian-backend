using Application.AccountStatements.Abstraction;
using Application.Agent.Abstractions;
using Application.Agent.Conversations;
using Application.Availabilities.Abstraction;
using Application.Appointments.Abstraction;
using Infrastructure.Appointments.BackgroundServices;
using Infrastructure.Appointments.Configuration;
using Application.AppointmentStatusHistories.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Diagnostics.Abstraction;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Modules.Abstraction;
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

using Application.MessageTypes.Abstraction;

using Application.EscalationStatuses.Abstraction;




using Application.Notifications.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;

using Infrastructure.AccountStatements.Repositories;
using Infrastructure.Agent.Configuration;
using Infrastructure.Agent.Conversations;
using Infrastructure.Agent.Http;
using Infrastructure.Notifications.Repositories;
using Infrastructure.Availabilities.Repositories;
using Infrastructure.Appointments.Repositories;
using Infrastructure.AppointmentStatusHistories.Repositories;
using Infrastructure.MedicalRecords.Repositories;
using Infrastructure.Vaccinations.Repositories;
using Infrastructure.Clients.Repositories;

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
using Application.UserTokens.Abstraction;
using Application.Telegram.Abstractions;
using Application.Telegram.Linking;
using Application.Telegram.Registration;
using Infrastructure.AgentHumans.Repository;
using Infrastructure.AiModels.Repository;
using Infrastructure.ChatConversationAssignments.Repository;
using Infrastructure.ChatConversationAiSettings.Repository;
using Infrastructure.ChatConversations.Repository;
using Infrastructure.ChatEscalationAssignments.Repository;
using Infrastructure.ChatEscalationResolutions.Repository;
using Infrastructure.ChatEscalations.Repository;
using Infrastructure.ChatEscalationStatusHistories.Repository;
using Infrastructure.ChatAiRunErrors.Repository;
using Infrastructure.ChatAiRunMetrics.Repository;
using Infrastructure.ChatAiRuns.Repository;
using Infrastructure.ChatAttachments.Repository;
using Infrastructure.ChatMessages.Repository;
using Infrastructure.ChatParticipants.Repository;
using Infrastructure.ChatUserProfiles.Repository;
using Infrastructure.ProviderModelsAi.Repository;

using Application.Security.Abstractions;
using Application.Security.Registration;
using Infrastructure.Diagnostics.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Pets.Repositories;
using Infrastructure.Races.Repositories;
using Infrastructure.Roles.Repository;
using Application.RolePermissions.Abstraction;
using Application.UserPermissions.Abstraction;

using Infrastructure.Modules.Repositories;

using Infrastructure.RolePermissions.Repositories;
using Infrastructure.UserPermissions.Repositories;
using Infrastructure.Security;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Infrastructure.Species.Repositories;
using Infrastructure.StatusAppointments.Repositories;
using Infrastructure.TypeServices.Repositories;

using Infrastructure.Services.Repositories;

using Infrastructure.Specialties.Repositories;
using Infrastructure.ClientsPets.Repositories;

using Infrastructure.Veterinarians.Repositories;
using Infrastructure.Priorities.Repositories;

using Infrastructure.SenderTypes.Repositories;

using Infrastructure.AiRunStatuses.Repositories;

using Infrastructure.ConversationStatuses.Repositories;

using Infrastructure.MessageTypes.Repositories;

using Infrastructure.EscalationStatuses.Repositories;




using Infrastructure.UserAccounts.Repository;
using Infrastructure.UserCredentials.Repositories;
using Infrastructure.Users.Repository;
using Infrastructure.UserTokens.Repositories;
using Infrastructure.Telegram;
using Infrastructure.Telegram.Repositories;
using Infrastructure.Telegram.Configuration;
using Infrastructure.Telegram.Security;
using Infrastructure.Telegram.Http;
using Infrastructure.Telegram.Workers;
using Infrastructure.Telegram.Identity;
using Infrastructure.Email;
using Infrastructure.Email.Configuration;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Oracle connection string is not configured.");

        services.AddDbContext<VeterinaryDbContext>(options =>
            options.UseOracle(connectionString, oracle =>
                // XE 21c no soporta booleanos nativos (default del provider 23).
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21)));

        services.AddScoped<IDiagnosticRepository, DiagnosticRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IRaceRepository, RaceRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        services.AddScoped<IRolesRepository, RolesRepository>();
        services.AddScoped<IModulesRepository, ModulesRepository>();
        services.AddScoped<IRolePermissionsRepository, RolePermissionsRepository>();
        services.AddScoped<IUserPermissionsRepository, UserPermissionsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IStatusAppointmentRepository, StatusAppointmentRepository>();
        services.AddScoped<ITypeServiceRepository, TypeServiceRepository>();

        services.AddScoped<IServiceRepository, ServiceRepository>();

        services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
        services.AddScoped<IClientPetRepository, ClientPetRepository>();

        services.AddScoped<IVeterinarianRepository, VeterinarianRepository>();

        services.AddScoped<IPriorityRepository, PriorityRepository>();

        services.AddScoped<ISenderTypeRepository, SenderTypeRepository>();

        services.AddScoped<IAiRunStatusRepository, AiRunStatusRepository>();

        services.AddScoped<IConversationStatusRepository, ConversationStatusRepository>();

        services.AddScoped<IMessageTypeRepository, MessageTypeRepository>();

        services.AddScoped<IEscalationStatusRepository, EscalationStatusRepository>();


        services.AddScoped<IUserAccountsRepository, UserAccountsRepository>();
        services.AddScoped<IUserCredentialsRepository, UserCredentialsRepository>();

        services.AddScoped<IClientRepository, ClientRepository>();

        services.AddScoped<IUserTokensRepository, UserTokensRepository>();
        services.AddScoped<IAccountStatementsRepository, AccountStatementsRepository>();
        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IAppointmentStatusHistoryRepository, AppointmentStatusHistoryRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
        services.AddScoped<IVaccinationRepository, VaccinationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IProviderModelAiRepository, ProviderModelAiRepository>();
        services.AddScoped<IAiModelRepository, AiModelRepository>();
        services.AddScoped<IChatUserProfileRepository, ChatUserProfileRepository>();
        services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
        services.AddScoped<IChatConversationAssignmentRepository, ChatConversationAssignmentRepository>();
        services.AddScoped<IChatConversationAiSettingRepository, ChatConversationAiSettingRepository>();
        services.AddScoped<IChatParticipantRepository, ChatParticipantRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IChatAttachmentRepository, ChatAttachmentRepository>();
        services.AddScoped<IChatEscalationRepository, ChatEscalationRepository>();
        services.AddScoped<IChatEscalationStatusHistoryRepository, ChatEscalationStatusHistoryRepository>();
        services.AddScoped<IChatEscalationResolutionRepository, ChatEscalationResolutionRepository>();
        services.AddScoped<IChatEscalationAssignmentRepository, ChatEscalationAssignmentRepository>();
        services.AddScoped<IChatAiRunRepository, ChatAiRunRepository>();
        services.AddScoped<IChatAiRunMetricsRepository, ChatAiRunMetricsRepository>();
        services.AddScoped<IChatAiRunErrorRepository, ChatAiRunErrorRepository>();
        services.AddScoped<IAgentHumanRepository, AgentHumanRepository>();
        services.AddScoped<ITelegramLinkCodeRepository, TelegramLinkCodeRepository>();
        services.AddScoped<ITelegramUserLinkRepository, TelegramUserLinkRepository>();
        services.AddScoped<ITelegramConversationLinkRepository, TelegramConversationLinkRepository>();
        services.AddScoped<ITelegramInboundUpdateRepository, TelegramInboundUpdateRepository>();
        services.AddScoped<ITelegramLinkingSessionRepository, TelegramLinkingSessionRepository>();
        services.AddScoped<ITelegramRegistrationSessionRepository, TelegramRegistrationSessionRepository>();
        services.AddScoped<ITelegramUnitOfWork, TelegramUnitOfWork>();
        services.AddScoped<TelegramUpdatePump>();
        services.AddSingleton<ITelegramUpdateSignal, InMemoryTelegramUpdateSignal>();
        services.AddScoped<ITelegramChatLinkingService, TelegramChatLinkingService>();
        services.AddScoped<ITelegramRegistrationService, TelegramRegistrationService>();
        services.AddSingleton<ITelegramLinkCodeProtector, TelegramLinkCodeProtector>();
        services.AddSingleton<ITelegramRegistrationProtector>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;
            return new TelegramRegistrationProtector(options.RegistrationProtectionKeyBase64);
        });
        services.AddSingleton<ITelegramOtpProtector>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;
            return new TelegramOtpProtector(options.OtpPepperBase64);
        });
        services.AddScoped<ITelegramAccountLookup, TelegramAccountLookup>();
        services.AddScoped<ITelegramRegistrationAccountLookup, TelegramRegistrationAccountLookup>();
        services.AddScoped<ITelegramVerificationCodeSender, SmtpTelegramVerificationCodeSender>();
        services.AddScoped<ISmtpTransport, SmtpTransport>();
        services.AddScoped<IAgentDelegatedIdentityProvider, AgentDelegatedIdentityProvider>();
        services.AddHttpClient<ITelegramBotClient, TelegramBotHttpClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(15);
        }).RemoveAllLoggers();

        services.AddSingleton<IValidateOptions<TelegramOptions>, TelegramOptionsValidator>();
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<ITelegramRuntimeSettings>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;
            return new ConfiguredTelegramRuntimeSettings(
                options.GuestModeEnabled,
                options.BotUsername,
                TimeSpan.FromMinutes(options.LinkCodeTtlMinutes),
                TimeSpan.FromMilliseconds(options.WorkerPollMilliseconds),
                TimeSpan.FromSeconds(options.ProcessingLeaseSeconds),
                options.MaxProcessingAttempts,
                TimeSpan.FromMinutes(options.DelegatedTokenMinutes),
                TimeSpan.FromMinutes(options.OtpTtlMinutes),
                options.OtpMaximumAttempts,
                TimeSpan.FromSeconds(options.OtpResendSeconds),
                options.RegistrationEnabled,
                options.RegistrationCompletionUrl,
                TimeSpan.FromMinutes(options.RegistrationOtpTtlMinutes),
                TimeSpan.FromMinutes(options.RegistrationTokenTtlMinutes),
                options.RegistrationMaxOtpAttempts,
                TimeSpan.FromSeconds(options.RegistrationResendSeconds));
        });

        var telegramOptions = configuration
            .GetSection(TelegramOptions.SectionName)
            .Get<TelegramOptions>() ?? new TelegramOptions();
        if (telegramOptions.Enabled)
        {
            services.AddHostedService<TelegramUpdateWorker>();
        }

        services.AddScoped<IUnitOfWork, Infrastructure.UnitOfWork.UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddSingleton<IValidateOptions<AgentOptions>, AgentOptionsValidator>();
        services.AddOptions<AgentOptions>()
            .Bind(configuration.GetSection(AgentOptions.SectionName))
            .ValidateOnStart();

        var agentOptions = configuration
            .GetSection(AgentOptions.SectionName)
            .Get<AgentOptions>() ?? new AgentOptions();
        if (agentOptions.Enabled)
        {
            services.AddScoped<IAgentConversationDefaults>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AgentOptions>>().Value;
                return new ConfiguredAgentConversationDefaults(
                    Guid.Parse(options.InitialConversationStatusId),
                    Guid.Parse(options.ClientParticipantTypeId));
            });
            services.AddScoped<
                IActiveConversationEscalationReader,
                ActiveConversationEscalationReader>();
            services.AddScoped<
                IConversationContextProvider,
                PersistentConversationContextProvider>();
            services.AddHttpClient<IAgentMessagingClient, AgentMessagingHttpClient>((provider, client) =>
            {
                var validated = provider.GetRequiredService<IOptions<AgentOptions>>().Value;
                client.BaseAddress = new Uri(validated.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(validated.RequestTimeoutSeconds);
            });
        }
        else
        {
            services.AddSingleton<
                IConversationContextProvider,
                DisabledConversationContextProvider>();
            services.AddSingleton<IAgentMessagingClient, DisabledAgentMessagingClient>();
        }

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<JwtRsaKeyMaterial>();
        services.AddSingleton<JwtTokenIssuer>();
        services.AddSingleton<RefreshTokenProtector>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IClientAccountRegistrationService, ClientAccountRegistrationService>();

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SuperAdminOptions>, SuperAdminOptionsValidator>();
        services.AddOptions<SuperAdminOptions>()
            .Bind(configuration.GetSection(SuperAdminOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<ReminderOptions>, ReminderOptionsValidator>();
        services.AddOptions<ReminderOptions>()
            .Bind(configuration.GetSection(ReminderOptions.SectionName))
            .ValidateOnStart();
        services.AddHostedService<AppointmentReminderBackgroundService>();

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
