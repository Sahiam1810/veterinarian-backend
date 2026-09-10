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
    public async Task STA_T17_Custom_role_without_Citas_Edit_permission_fails()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Citas", PermissionAction.Edit);
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "GestorSinPermisos")],
            authenticationType: "TestAuth");

        var context = new AuthorizationHandlerContext([requirement], new System.Security.Claims.ClaimsPrincipal(identity), null);
        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
