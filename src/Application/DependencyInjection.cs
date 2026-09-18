using Application.Agent.Abstractions;
using Application.Agent.Messages;
using Application.Common.Validators;
using Application.Owners.Abstractions;
using Application.Owners.Adapters;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
        services.AddScoped<IAgentMessageDispatcher, AgentMessageDispatcher>();
        services.AddScoped<IRegisterOwnerFromStaff, RegisterOwnerFromStaff>();
        services.AddScoped<IRegisterOwnerFromBot, RegisterOwnerFromBot>();
        services.AddScoped<IRegisterOwnerFromTelegram, RegisterOwnerFromTelegram>();
        services.AddScoped<IRegisterOwnerCleanup, RegisterOwnerCleanupStub>();

        return services;
    }
}
