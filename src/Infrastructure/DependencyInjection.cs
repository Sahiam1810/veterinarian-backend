using Application.Diagnostics.Abstraction;
using Application.Diagnostics.UseCases;
using Infrastructure.Diagnostics.Repositories;
using Infrastructure.Persistence;
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

        // Repositorios
        services.AddScoped<IDiagnosticRepository, DiagnosticRepository>();

        // Casos de Uso
        services.AddScoped<GetAllDiagnosticsUseCase>();
        services.AddScoped<GetDiagnosticByIdUseCase>();
        services.AddScoped<CreateDiagnosticUseCase>();
        services.AddScoped<UpdateDiagnosticUseCase>();
        services.AddScoped<DeleteDiagnosticUseCase>();

        return services;
    }
}
