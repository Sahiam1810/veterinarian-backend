using Api.Agent.Security;
using Application.Agent.Abstractions;

namespace Api.Agent;

public static class DependencyInjection
{
    public static IServiceCollection AddAgentApi(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserAccessTokenProvider, HttpContextUserAccessTokenProvider>();
        return services;
    }
}
