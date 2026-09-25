using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Api.Appointments.Controllers;
using Api.AppointmentStatusHistories.Controllers;
using Api.Availabilities.Controllers;
using Api.ClientsPets.Controllers;
using Api.Common.Security.Permissions;
using Api.Pets.Controllers;
using Api.VeterinarianAbsences.Controllers;
using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// Lecturas de dominio alineadas permission-first:
// Pets/ClientsPets → Mascotas.View; Appointments/Availabilities/Absences → Citas.View.
public sealed class ModuleViewAuthorizationTests
{
    private readonly PermissionAuthorizationHandler handler = new();

    public static IEnumerable<object[]> MascotasViewActions()
    {
        yield return [typeof(PetsController), nameof(PetsController.GetAll)];
        yield return [typeof(PetsController), nameof(PetsController.GetById)];
        yield return [typeof(ClientsPetsController), nameof(ClientsPetsController.GetAll)];
        yield return [typeof(ClientsPetsController), nameof(ClientsPetsController.GetById)];
    }

    public static IEnumerable<object[]> CitasViewActions()
    {
        yield return [typeof(AppointmentsController), nameof(AppointmentsController.GetAll)];
        yield return [typeof(AppointmentsController), nameof(AppointmentsController.GetById)];
        yield return [typeof(AppointmentStatusHistoriesController), nameof(AppointmentStatusHistoriesController.GetAll)];
        yield return [typeof(AppointmentStatusHistoriesController), nameof(AppointmentStatusHistoriesController.GetById)];
        yield return [typeof(AvailabilitiesController), nameof(AvailabilitiesController.GetAll)];
        yield return [typeof(AvailabilitiesController), nameof(AvailabilitiesController.GetById)];
        yield return [typeof(AvailabilitiesController), nameof(AvailabilitiesController.GetAvailableSlots)];
        yield return [typeof(AvailabilitiesController), nameof(AvailabilitiesController.GetByVeterinarianId)];
        yield return [typeof(VeterinarianAbsencesController), nameof(VeterinarianAbsencesController.GetAll)];
        yield return [typeof(VeterinarianAbsencesController), nameof(VeterinarianAbsencesController.GetById)];
        yield return [typeof(VeterinarianAbsencesController), nameof(VeterinarianAbsencesController.GetByVeterinarianId)];
    }

    [Theory]
    [MemberData(nameof(MascotasViewActions))]
    public void Mascotas_read_actions_require_Mascotas_View(Type controllerType, string methodName)
    {
        AssertRequiresModuleView(controllerType, methodName, "Mascotas");
    }

    [Theory]
    [MemberData(nameof(CitasViewActions))]
    public void Citas_read_actions_require_Citas_View(Type controllerType, string methodName)
    {
        AssertRequiresModuleView(controllerType, methodName, "Citas");
    }

    [Fact]
    public async Task Auxiliar_with_Mascotas_View_can_read_pets()
    {
        var requirement = new PermissionRequirement("Mascotas", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [
                new Claim(ClaimTypes.Role, "Auxiliar"),
                new Claim(PermissionClaimValue.ClaimType, "perm:Mascotas:View"),
            ]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Auxiliar_without_Mascotas_View_cannot_read_pets()
    {
        var requirement = new PermissionRequirement("Mascotas", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [new Claim(ClaimTypes.Role, "Auxiliar")]);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Auxiliar_with_Citas_View_can_read_appointments_and_slots()
    {
        var requirement = new PermissionRequirement("Citas", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [
                new Claim(ClaimTypes.Role, "Auxiliar"),
                new Claim(PermissionClaimValue.ClaimType, "perm:Citas:View"),
            ]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Auxiliar_with_only_Plataforma_View_cannot_read_pets()
    {
        var requirement = new PermissionRequirement("Mascotas", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [
                new Claim(ClaimTypes.Role, "Auxiliar"),
                new Claim(PermissionClaimValue.ClaimType, "perm:Plataforma:View"),
            ]);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static void AssertRequiresModuleView(Type controllerType, string methodName, string module)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>().ToArray();
        var authorizeAttribute = Assert.Single(authorizeAttributes);
        Assert.Equal($"perm:{module}:{PermissionAction.View}", authorizeAttribute.Policy);
        Assert.IsType<RequirePermissionAttribute>(authorizeAttribute);
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
