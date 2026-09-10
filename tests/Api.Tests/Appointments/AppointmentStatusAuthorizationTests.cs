using System.Reflection;
using Api.AppointmentStatusHistories.Controllers;
using Api.Appointments.Controllers;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Appointments;

public sealed class AppointmentStatusAuthorizationTests
{
    [Fact]
    public void STA_T16_AppointmentsController_UpdateStatus_requires_Citas_Edit_permission()
    {
        var method = typeof(AppointmentsController).GetMethod(nameof(AppointmentsController.UpdateStatus));
        Assert.NotNull(method);

        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"perm:Citas:{PermissionAction.Edit}", attr.Policy);
    }

    [Theory]
    [InlineData(nameof(AppointmentStatusHistoriesController.Create))]
    [InlineData(nameof(AppointmentStatusHistoriesController.Update))]
    [InlineData(nameof(AppointmentStatusHistoriesController.Delete))]
    public void STA_T17_AppointmentStatusHistories_mutations_require_Citas_Edit_permission(string methodName)
    {
        var method = typeof(AppointmentStatusHistoriesController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>().ToArray();
        var authorizeAttribute = Assert.Single(authorizeAttributes);
        Assert.IsType<RequirePermissionAttribute>(authorizeAttribute);
        Assert.Equal($"perm:Citas:{PermissionAction.Edit}", authorizeAttribute.Policy);
    }

    [Theory]
    [InlineData(nameof(AppointmentStatusHistoriesController.GetAll))]
    [InlineData(nameof(AppointmentStatusHistoriesController.GetById))]
    public void STA_T18_AppointmentStatusHistories_reads_require_Plataforma_View_permission(string methodName)
    {
        var method = typeof(AppointmentStatusHistoriesController).GetMethod(methodName);
        Assert.NotNull(method);

        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"perm:Plataforma:{PermissionAction.View}", attr.Policy);
    }

    [Fact]
    public async Task STA_T17_Custom_role_with_Citas_Edit_permission_succeeds_without_AdminOnly()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Citas", PermissionAction.Edit);
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "GestorCitasPersonalizado"),
                new System.Security.Claims.Claim(PermissionClaimValue.ClaimType, "perm:Citas:Edit")
            ],
            authenticationType: "TestAuth");

        var context = new AuthorizationHandlerContext([requirement], new System.Security.Claims.ClaimsPrincipal(identity), null);
        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void AppointmentsController_Update_requires_ReprogramacionDeCitas_Edit_permission()
    {
        var method = typeof(AppointmentsController).GetMethod(nameof(AppointmentsController.Update));
        Assert.NotNull(method);

        var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>().ToArray();
        var authorizeAttribute = Assert.Single(authorizeAttributes);
        Assert.IsType<RequirePermissionAttribute>(authorizeAttribute);
        Assert.Equal($"perm:Reprogramación de Citas:{PermissionAction.Edit}", authorizeAttribute.Policy);
    }

    [Fact]
    public async Task Veterinarian_with_Citas_Edit_can_update_status_but_cannot_reschedule_appointment()
    {
        var handler = new PermissionAuthorizationHandler();
        var statusRequirement = new PermissionRequirement("Citas", PermissionAction.Edit);
        var rescheduleRequirement = new PermissionRequirement("Reprogramación de Citas", PermissionAction.Edit);

        var vetClaims = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Veterinario"),
                new System.Security.Claims.Claim(PermissionClaimValue.ClaimType, "perm:Citas:Edit")
            ],
            authenticationType: "TestAuth");

        var principal = new System.Security.Claims.ClaimsPrincipal(vetClaims);

        // Puede cambiar estado (PATCH /{id}/status)
        var statusContext = new AuthorizationHandlerContext([statusRequirement], principal, null);
        await handler.HandleAsync(statusContext);
        Assert.True(statusContext.HasSucceeded);

        // NO puede reprogramar (PUT /{id})
        var rescheduleContext = new AuthorizationHandlerContext([rescheduleRequirement], principal, null);
        await handler.HandleAsync(rescheduleContext);
        Assert.False(rescheduleContext.HasSucceeded);
    }

    [Fact]
    public async Task Receptionist_with_ReprogramacionDeCitas_Edit_can_reschedule_appointment()
    {
        var handler = new PermissionAuthorizationHandler();
        var rescheduleRequirement = new PermissionRequirement("Reprogramación de Citas", PermissionAction.Edit);

        var receptionistClaims = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Recepcionista"),
                new System.Security.Claims.Claim(PermissionClaimValue.ClaimType, "perm:Reprogramación de Citas:Edit")
            ],
            authenticationType: "TestAuth");

        var rescheduleContext = new AuthorizationHandlerContext(
            [rescheduleRequirement],
            new System.Security.Claims.ClaimsPrincipal(receptionistClaims),
            null);
        await handler.HandleAsync(rescheduleContext);

        Assert.True(rescheduleContext.HasSucceeded);
    }
}
