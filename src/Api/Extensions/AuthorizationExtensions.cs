using Api.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace Api.Extensions;

public static class AuthorizationExtensions
{
    // Los nombres de rol comparados aquí son los mismos que llegan en el
    // claim "role" del JWT (poblado en el login desde la tabla ROLES, no
    // desde un catálogo fijo en código). El catálogo de roles en sí sigue
    // siendo 100% administrable desde la base de datos vía el módulo Roles.
    public static IServiceCollection AddApiAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(
                AuthorizationPolicies.AdminOnly,
                policy => policy.RequireRole("Administrador"));
            options.AddPolicy(
                AuthorizationPolicies.VeterinarianOnly,
                policy => policy.RequireRole("Veterinario"));
            options.AddPolicy(
                AuthorizationPolicies.ReceptionistOnly,
                policy => policy.RequireRole("Recepcionista"));
            options.AddPolicy(
                AuthorizationPolicies.AssistantOnly,
                policy => policy.RequireRole("Auxiliar"));
            options.AddPolicy(
                AuthorizationPolicies.ClientOnly,
                policy => policy.RequireRole("Cliente"));
            options.AddPolicy(
                AuthorizationPolicies.StaffOnly,
                policy => policy.RequireRole(
                    "Administrador",
                    "Veterinario",
                    "Recepcionista",
                    "Auxiliar"));
        });

        return services;
    }
}