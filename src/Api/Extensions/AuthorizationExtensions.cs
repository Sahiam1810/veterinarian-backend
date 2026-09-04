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
                RoleOrSuperAdmin("Administrador"));
            // SuperAdmin es un rol persistido identificado por su role_id canónico.
            options.AddPolicy(
                AuthorizationPolicies.SuperAdminOnly,
                policy => policy.RequireAssertion(context => context.User.IsSuperAdmin()));
            options.AddPolicy(
                AuthorizationPolicies.VeterinarianOnly,
                RoleOrSuperAdmin("Veterinario"));
            options.AddPolicy(
                AuthorizationPolicies.ReceptionistOnly,
                RoleOrSuperAdmin("Recepcionista"));
            options.AddPolicy(
                AuthorizationPolicies.AssistantOnly,
                RoleOrSuperAdmin("Auxiliar"));
            options.AddPolicy(
                AuthorizationPolicies.ClientOnly,
                RoleOrSuperAdmin("Cliente"));
            options.AddPolicy(
                AuthorizationPolicies.StaffOnly,
                RoleOrSuperAdmin(
                    "Administrador",
                    "Veterinario",
                    "Recepcionista",
                    "Auxiliar"));

            // Políticas combinadas: acciones que corresponden a más de un rol.
            options.AddPolicy(
                AuthorizationPolicies.AdminOrReceptionist,
                RoleOrSuperAdmin("Administrador", "Recepcionista"));
            options.AddPolicy(
                AuthorizationPolicies.AdminOrVeterinarian,
                RoleOrSuperAdmin("Administrador", "Veterinario"));
            options.AddPolicy(
                AuthorizationPolicies.ClinicalStaffOnly,
                RoleOrSuperAdmin(
                    "Administrador",
                    "Veterinario",
                    "Recepcionista"));
            options.AddPolicy(
                AuthorizationPolicies.FrontDeskStaffOnly,
                RoleOrSuperAdmin(
                    "Administrador",
                    "Recepcionista",
                    "Auxiliar"));
            options.AddPolicy(
                AuthorizationPolicies.ClinicalHistoryReadOnly,
                RoleOrSuperAdmin(
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

    // El SuperAdmin no tiene fila en ROLES (no es un rol, es un claim a nivel
    // de usuario): cualquier policy basada en rol debe dejarlo pasar igual,
    // sin que cada endpoint tenga que saber de su existencia.
    private static Action<AuthorizationPolicyBuilder> RoleOrSuperAdmin(params string[] roles) =>
        policy => policy.RequireAssertion(context =>
            roles.Any(context.User.IsInRole) ||
            context.User.IsSuperAdmin());
}
