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
using Application.SenderTypes.Abstraction;

using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;

using Infrastructure.Clients.Repositories;

using Application.AiModels.Abstraction;
using Application.ProviderModelsAi.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.AiModels.Repository;
using Infrastructure.ProviderModelsAi.Repository;

using Infrastructure.Diagnostics.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Pets.Repositories;
using Infrastructure.Races.Repositories;
using Infrastructure.Roles.Repository;
using Infrastructure.Security;
using Infrastructure.Species.Repositories;
using Infrastructure.StatusAppointments.Repositories;
using Infrastructure.TypeServices.Repositories;

using Infrastructure.Services.Repositories;

using Infrastructure.Specialties.Repositories;
using Infrastructure.ClientsPets.Repositories;
using Infrastructure.SenderTypes.Repositories;

using Infrastructure.UserAccounts.Repository;
using Infrastructure.UserCredentials.Repositories;
using Infrastructure.Users.Repository;
using Infrastructure.UserTokens.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<ISenderTypeRepository, SenderTypeRepository>();

        services.AddScoped<IUserAccountsRepository, UserAccountsRepository>();
        services.AddScoped<IUserCredentialsRepository, UserCredentialsRepository>();

        services.AddScoped<IClientRepository, ClientRepository>();

        services.AddScoped<IUserTokensRepository, UserTokensRepository>();
        services.AddScoped<IProviderModelAiRepository, ProviderModelAiRepository>();
        services.AddScoped<IAiModelRepository, AiModelRepository>();

        services.AddScoped<IUnitOfWork, Infrastructure.UnitOfWork.UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

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
