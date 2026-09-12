using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Api.AccountStatements.Controllers;
using Api.Appointments.Controllers;
using Api.AppointmentStatusHistories.Controllers;
using Api.Availabilities.Controllers;
using Api.ClientsPets.Controllers;
using Api.Common.Security.Permissions;
using Api.Modules.Controllers;
using Api.Pets.Controllers;
using Api.Priorities.Controllers;
using Api.VeterinarianAbsences.Controllers;
using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// S8.2: StaffOnly (allowlist fijo de 4 nombres de rol) reemplazado por el
// permiso de matriz Plataforma:View en los 9 controllers que antes lo usaban.
// Un rol nuevo/configurable con Plataforma:AccesoWeb concedido ahora pasa
// estos endpoints sin que su nombre tenga que coincidir con uno de los 4
// conocidos (Administrador/Veterinario/Recepcionista/Auxiliar).
public sealed class PlatformAccessAuthorizationTests
{
    private readonly PermissionAuthorizationHandler handler = new();

    public static IEnumerable<object[]> MigratedControllerActions()
    {
        yield return [typeof(AccountStatementsController), nameof(AccountStatementsController.GetById)];
        yield return [typeof(AppointmentsController), nameof(AppointmentsController.GetAll)];
        yield return [typeof(AppointmentStatusHistoriesController), nameof(AppointmentStatusHistoriesController.GetAll)];
        yield return [typeof(AvailabilitiesController), nameof(AvailabilitiesController.GetAll)];
        yield return [typeof(ClientsPetsController), nameof(ClientsPetsController.GetAll)];
        yield return [typeof(ModulesController), nameof(ModulesController.GetAll)];
        yield return [typeof(PetsController), nameof(PetsController.GetAll)];
        yield return [typeof(PrioritiesController), nameof(PrioritiesController.GetAll)];
        yield return [typeof(VeterinarianAbsencesController), nameof(VeterinarianAbsencesController.GetAll)];
    }

    [Theory]
    [MemberData(nameof(MigratedControllerActions))]
    public void Migrated_action_requires_the_Plataforma_View_permission_instead_of_StaffOnly(
        Type controllerType,
        string methodName)
    {
        var method = controllerType.GetMethod(methodName);

        Assert.NotNull(method);

        // RequirePermissionAttribute hereda de AuthorizeAttribute (ver RequirePermissionAttribute.cs);
        // solo debe existir esta una instancia, con el policy de matriz "perm:Plataforma:View" y
        // no el literal "StaffOnly" que usaba [Authorize(Policy = AuthorizationPolicies.StaffOnly)].
        var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>().ToArray();
        var authorizeAttribute = Assert.Single(authorizeAttributes);
        Assert.Equal($"perm:Plataforma:{PermissionAction.View}", authorizeAttribute.Policy);
        Assert.IsType<RequirePermissionAttribute>(authorizeAttribute);
    }

    // Sin JWT: la falta de autenticación la corta el middleware de JwtBearer antes de
    // llegar acá (401). Este handler cubre el otro criterio: un rol autenticado con
    // (o sin) el claim puntual Plataforma:View (403 si falta).
    [Fact]
    public async Task Succeeds_for_a_custom_role_with_the_Plataforma_View_claim()
    {
        var requirement = new PermissionRequirement("Plataforma", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [new Claim(PermissionClaimValue.ClaimType, "perm:Plataforma:View")]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_for_a_role_without_the_Plataforma_View_claim()
    {
        var requirement = new PermissionRequirement("Plataforma", PermissionAction.View);
        var context = CreateContext(requirement, []);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateContext(
        PermissionRequirement requirement,
        IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(identity),
            resource: null);
    }
}
