using System.Reflection;
using System.Security.Claims;
using Api.Common.Security.Permissions;
using Api.Diagnostics.Controllers;
using Api.Roles.Controllers;
using Application.Permissions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

public sealed class RolesAndDiagnosticsAuthorizationTests
{
    private static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PersonId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ISender sender = Substitute.For<ISender>();
    private readonly PermissionAuthorizationHandler handler;

    public RolesAndDiagnosticsAuthorizationTests()
    {
        handler = new PermissionAuthorizationHandler(sender);
    }

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
    public async Task SuperAdmin_bypasses_matrix_and_gained_access_to_Roles_module()
    {
        var requirement = new PermissionRequirement("Roles", PermissionAction.View);
        var context = CreateContext(
            requirement,
            claims: [new Claim("super_admin", "true")]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        await sender.DidNotReceive().Send(Arg.Any<GetEffectivePermissionQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Regular_role_without_explicit_roles_permission_is_denied_access_to_Roles_module()
    {
        sender
            .Send(Arg.Is<GetEffectivePermissionQuery>(q => q.ModuleName == "Roles"), Arg.Any<CancellationToken>())
            .Returns(EffectivePermission.None);

        var requirement = new PermissionRequirement("Roles", PermissionAction.View);
        var context = CreateContext(
            requirement,
            claims:
            [
                new Claim("role_id", AdminRoleId.ToString()),
                new Claim("person_id", PersonId.ToString())
            ]);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Diagnostics_GetAll_succeeds_when_user_has_HistorialesClinicos_View_permission()
    {
        sender
            .Send(Arg.Is<GetEffectivePermissionQuery>(q => q.ModuleName == "Historiales Clínicos"), Arg.Any<CancellationToken>())
            .Returns(new EffectivePermission(CanView: true, CanCreate: false, CanEdit: false, CanDelete: false));

        var requirement = new PermissionRequirement("Historiales Clínicos", PermissionAction.View);
        var context = CreateContext(
            requirement,
            claims:
            [
                new Claim("role_id", AdminRoleId.ToString()),
                new Claim("person_id", PersonId.ToString())
            ]);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Diagnostics_GetAll_fails_when_user_lacks_HistorialesClinicos_View_permission()
    {
        sender
            .Send(Arg.Is<GetEffectivePermissionQuery>(q => q.ModuleName == "Historiales Clínicos"), Arg.Any<CancellationToken>())
            .Returns(EffectivePermission.None);

        var requirement = new PermissionRequirement("Historiales Clínicos", PermissionAction.View);
        var context = CreateContext(
            requirement,
            claims:
            [
                new Claim("role_id", AdminRoleId.ToString()),
                new Claim("person_id", PersonId.ToString())
            ]);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateContext(
        PermissionRequirement requirement,
        IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }
}
