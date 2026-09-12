using System.Reflection;
using System.Security.Claims;
using Api.Common.Security.Permissions;
using Api.Reports.Controllers;
using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Reports;

public sealed class ReportsAuthorizationTests
{
    private readonly PermissionAuthorizationHandler handler = new();

    [Fact]
    public void Appointments_by_veterinarian_requires_the_Reportes_view_permission()
    {
        var action = typeof(ReportsController).GetMethod(nameof(ReportsController.GetAppointmentsByVeterinarian));

        Assert.NotNull(action);
        var permission = action.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal("perm:Reportes:View", permission.Policy);
        Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void GetAppointmentsByStatus_requires_Reportes_View_permission()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetAppointmentsByStatus));
        Assert.NotNull(method);

        var permission = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal("perm:Reportes:View", permission.Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void GetTopServices_requires_Reportes_View_permission()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetTopServices));
        Assert.NotNull(method);

        var permission = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal("perm:Reportes:View", permission.Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void GetSummary_requires_Reportes_View_permission()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetSummary));
        Assert.NotNull(method);

        var permission = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal("perm:Reportes:View", permission.Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    // Sin JWT: la falta de autenticación la corta el middleware de JwtBearer antes de
    // llegar acá (401, ver JwtBearerAuthenticationTests). Este handler cubre el otro
    // criterio: autenticado pero sin el permiso puntual (403).
    [Fact]
    public async Task Appointments_by_veterinarian_succeeds_with_the_exact_Reportes_View_claim()
    {
        var requirement = new PermissionRequirement("Reportes", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [new Claim(PermissionClaimValue.ClaimType, "perm:Reportes:View")]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Appointments_by_veterinarian_fails_without_the_required_claim()
    {
        var requirement = new PermissionRequirement("Reportes", PermissionAction.View);
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
