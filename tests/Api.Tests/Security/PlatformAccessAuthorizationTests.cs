using System.Reflection;
using System.Security.Claims;
using Api.Common.Security.Permissions;
using Api.Modules.Controllers;
using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

public sealed class PlatformAccessAuthorizationTests
{
    private readonly PermissionAuthorizationHandler handler = new();

    public static IEnumerable<object[]> PlatformViewControllerActions()
    {
        yield return [typeof(ModulesController), nameof(ModulesController.GetAll)];
    }

    [Theory]
    [MemberData(nameof(PlatformViewControllerActions))]
    public void Platform_catalog_action_requires_Plataforma_View(
        Type controllerType,
        string methodName)
    {
        var method = controllerType.GetMethod(methodName);

        Assert.NotNull(method);

        var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>().ToArray();
        var authorizeAttribute = Assert.Single(authorizeAttributes);
        Assert.Equal($"perm:Plataforma:{PermissionAction.View}", authorizeAttribute.Policy);
        Assert.IsType<RequirePermissionAttribute>(authorizeAttribute);
    }

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
