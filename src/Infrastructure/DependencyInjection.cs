using Application.Common.Abstractions;
using Application.Diagnostics.Abstraction;
using Application.Diagnostics.UseCases;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.Species.Abstraction;
using Infrastructure.Diagnostics.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Races.Repositories;
using Infrastructure.Roles.Repository;
using Infrastructure.Species.Repositories;
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
        services.AddScoped<IRaceRepository, RaceRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        services.AddScoped<IRolesRepository, RolesRepository>();
        services.AddScoped<IUnitOfWork, Infrastructure.UnitOfWork.UnitOfWork>();

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
