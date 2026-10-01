using System.Security.Claims;
using Api.Common.Security.Permissions;
using Application.Permissions.Claims;
using Domain.Roles;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

public sealed class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler sut = new();

    [Fact]
    public async Task HandleAsync_succeeds_for_super_admin_without_permission_claims()
    {
        var context = CreateContext(
            new PermissionRequirement("Citas", PermissionAction.Delete),
            [new Claim("role_id", SystemRoles.SuperAdminId.ToString())]);

        await sut.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_succeeds_for_the_exact_permission_claim()
    {
        var context = CreateContext(
            new PermissionRequirement("Citas", PermissionAction.Edit),
            [new Claim(PermissionClaimValue.ClaimType, "perm:Citas:Edit")]);

        await sut.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_fails_closed_when_the_permission_claim_is_missing()
    {
        var context = CreateContext(
            new PermissionRequirement("Citas", PermissionAction.View),
            []);

        await sut.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData("perm:Mascotas:Edit")]
    [InlineData("perm:Citas:View")]
    [InlineData("perm:citas:Edit")]
    [InlineData("prefix-perm:Citas:Edit-suffix")]
    public async Task HandleAsync_rejects_non_exact_permission_values(string permission)
    {
        var context = CreateContext(
            new PermissionRequirement("Citas", PermissionAction.Edit),
            [new Claim(PermissionClaimValue.ClaimType, permission)]);

        await sut.HandleAsync(context);

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
