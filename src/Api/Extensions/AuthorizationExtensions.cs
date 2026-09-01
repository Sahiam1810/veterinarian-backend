using Api.Common.Security;
using Api.Common.Security.Permissions;
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
            // El SuperAdmin no es un rol de la tabla ROLES: se identifica por
            // el claim "super_admin" que emite JwtTokenIssuer.IssueForSuperAdmin.
            options.AddPolicy(
                AuthorizationPolicies.SuperAdminOnly,
                policy => policy.RequireClaim("super_admin", "true"));
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

            // Políticas combinadas: acciones que corresponden a más de un rol.
            options.AddPolicy(
                AuthorizationPolicies.AdminOrReceptionist,
                policy => policy.RequireRole("Administrador", "Recepcionista"));
            options.AddPolicy(
                AuthorizationPolicies.AdminOrVeterinarian,
                policy => policy.RequireRole("Administrador", "Veterinario"));
            options.AddPolicy(
                AuthorizationPolicies.ClinicalStaffOnly,
                policy => policy.RequireRole(
                    "Administrador",
                    "Veterinario",
                    "Recepcionista"));
            options.AddPolicy(
                AuthorizationPolicies.FrontDeskStaffOnly,
                policy => policy.RequireRole(
                    "Administrador",
                    "Recepcionista",
                    "Auxiliar"));
            options.AddPolicy(
                AuthorizationPolicies.ClinicalHistoryReadOnly,
                policy => policy.RequireRole(
                    "Administrador",
                    "Veterinario",
                    "Recepcionista",
                    "Cliente"));
        });

        // Habilita las policies dinámicas "perm:{módulo}:{acción}" usadas por
        // [RequirePermission(...)], resueltas contra la tabla ROLE_PERMISSIONS.
        // Se registra después de AddAuthorization para que este provider
        // reemplace al DefaultAuthorizationPolicyProvider.
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        return services;
    }
}