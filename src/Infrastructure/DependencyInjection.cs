using Application.AccountStatements.Abstraction;
using Application.Availabilities.Abstraction;
using Application.Appointments.Abstraction;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Diagnostics.Abstraction;
using Application.Diagnostics.UseCases;
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

using Application.MessageTypes.Abstraction;

using Application.EscalationStatuses.Abstraction;




using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;

using Infrastructure.AccountStatements.Repositories;
using Infrastructure.Availabilities.Repositories;
using Infrastructure.Appointments.Repositories;
using Infrastructure.Clients.Repositories;

using Application.AgentHumans.Abstraction;
using Application.AiModels.Abstraction;
using Application.ChatUserProfiles.Abstraction;
using Application.ProviderModelsAi.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.AgentHumans.Repository;
using Infrastructure.AiModels.Repository;
using Infrastructure.ChatUserProfiles.Repository;
using Infrastructure.ProviderModelsAi.Repository;

using Application.Security.Abstractions;
using Infrastructure.Diagnostics.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Pets.Repositories;
using Infrastructure.Races.Repositories;
using Infrastructure.Roles.Repository;
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
            options.UseOracle(connectionString));

        services.AddScoped<IDiagnosticRepository, DiagnosticRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IRaceRepository, RaceRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        services.AddScoped<IRolesRepository, RolesRepository>();
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
        services.AddScoped<IProviderModelAiRepository, ProviderModelAiRepository>();
        services.AddScoped<IAiModelRepository, AiModelRepository>();
        services.AddScoped<IChatUserProfileRepository, ChatUserProfileRepository>();
        services.AddScoped<IAgentHumanRepository, AgentHumanRepository>();

        services.AddScoped<IUnitOfWork, Infrastructure.UnitOfWork.UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<JwtTokenIssuer>();
        services.AddSingleton<RefreshTokenProtector>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddScoped<GetAllDiagnosticsUseCase>();
        services.AddScoped<GetDiagnosticByIdUseCase>();
        services.AddScoped<CreateDiagnosticUseCase>();
        services.AddScoped<UpdateDiagnosticUseCase>();
        services.AddScoped<DeleteDiagnosticUseCase>();

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
