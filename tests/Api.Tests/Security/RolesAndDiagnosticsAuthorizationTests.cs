using System.Reflection;
using System.Security.Claims;
using Api.Common.Security.Permissions;
using Api.Diagnostics.Controllers;
using Api.Roles.Controllers;
using Application.Permissions.Claims;
using Domain.Roles;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

public sealed class RolesAndDiagnosticsAuthorizationTests
{
    private readonly PermissionAuthorizationHandler handler = new();

    [Fact]
    public void RolesController_does_not_have_class_level_authorize_attribute()
    {
        var classAuthorizeAttr = typeof(RolesController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.Null(classAuthorizeAttr);
    }

    [Theory]
    [InlineData(nameof(RolesController.Create), "Roles", PermissionAction.Create)]
    [InlineData(nameof(RolesController.GetAll), "Roles", PermissionAction.View)]
    [InlineData(nameof(RolesController.GetById), "Roles", PermissionAction.View)]
    [InlineData(nameof(RolesController.Update), "Roles", PermissionAction.Edit)]
    [InlineData(nameof(RolesController.Delete), "Roles", PermissionAction.Delete)]
    public void RolesController_actions_have_require_permission_attribute(
        string methodName,
        string expectedModule,
        PermissionAction expectedAction)
    {
        var method = typeof(RolesController).GetMethod(methodName);
        Assert.NotNull(method);

        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"perm:{expectedModule}:{expectedAction}", attr.Policy);
    }

    [Theory]
    [InlineData(nameof(DiagnosticsController.GetAll), "Historiales Clínicos", PermissionAction.View)]
    [InlineData(nameof(DiagnosticsController.GetById), "Historiales Clínicos", PermissionAction.View)]
    [InlineData(nameof(DiagnosticsController.Create), "Historiales Clínicos", PermissionAction.Create)]
    [InlineData(nameof(DiagnosticsController.Update), "Historiales Clínicos", PermissionAction.Edit)]
    [InlineData(nameof(DiagnosticsController.Delete), "Historiales Clínicos", PermissionAction.Delete)]
    public void DiagnosticsController_actions_have_require_permission_attribute(
        string methodName,
        string expectedModule,
        PermissionAction expectedAction)
    {
        var method = typeof(DiagnosticsController).GetMethod(methodName);
        Assert.NotNull(method);

        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal($"perm:{expectedModule}:{expectedAction}", attr.Policy);
    }

    [Fact]
    public async Task SuperAdmin_bypasses_matrix_and_gains_access_to_Roles_module()
    {
        var requirement = new PermissionRequirement("Roles", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [new Claim("role_id", SystemRoles.SuperAdminId.ToString())]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Regular_role_without_explicit_roles_permission_is_denied_access_to_Roles_module()
    {
        var requirement = new PermissionRequirement("Roles", PermissionAction.View);
        var context = CreateContext(requirement, []);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Diagnostics_GetAll_succeeds_with_the_exact_HistorialesClinicos_View_claim()
    {
        var requirement = new PermissionRequirement("Historiales Clínicos", PermissionAction.View);
        var context = CreateContext(
            requirement,
            [
                new Claim(
                    PermissionClaimValue.ClaimType,
                    "perm:Historiales Clínicos:View")
            ]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Diagnostics_GetAll_fails_without_the_required_claim()
    {
        var requirement = new PermissionRequirement("Historiales Clínicos", PermissionAction.View);
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
