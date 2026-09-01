using System.Security.Claims;
using Api.Common.Security.Permissions;
using Application.Permissions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

public sealed class PermissionAuthorizationHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PersonId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ISender sender = Substitute.For<ISender>();
    private readonly PermissionAuthorizationHandler sut;

    public PermissionAuthorizationHandlerTests()
    {
        sut = new PermissionAuthorizationHandler(sender);
    }

    [Fact]
    public async Task HandleAsync_succeeds_for_super_admin_without_consulting_effective_permissions()
    {
        var context = CreateContext(
            requirement: new PermissionRequirement("Citas", PermissionAction.Delete),
            claims: [new Claim("super_admin", "true")]);

        await sut.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        await sender.DidNotReceive().Send(Arg.Any<GetEffectivePermissionQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_fails_closed_when_there_is_no_role_id_claim()
    {
        var context = CreateContext(
            requirement: new PermissionRequirement("Citas", PermissionAction.View),
            claims: []);

        await sut.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        await sender.DidNotReceive().Send(Arg.Any<GetEffectivePermissionQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_succeeds_when_the_effective_permission_grants_the_requested_action()
    {
        sender
            .Send(Arg.Is<GetEffectivePermissionQuery>(q => q.RoleId == RoleId && q.UserId == PersonId && q.ModuleName == "Citas"), Arg.Any<CancellationToken>())
            .Returns(new EffectivePermission(CanView: true, CanCreate: false, CanEdit: false, CanDelete: false));

        var context = CreateContext(
            requirement: new PermissionRequirement("Citas", PermissionAction.View),
            claims:
            [
                new Claim("role_id", RoleId.ToString()),
                new Claim("person_id", PersonId.ToString())
            ]);

        await sut.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_does_not_succeed_when_the_effective_permission_denies_the_requested_action()
    {
        sender
            .Send(Arg.Any<GetEffectivePermissionQuery>(), Arg.Any<CancellationToken>())
            .Returns(new EffectivePermission(CanView: true, CanCreate: false, CanEdit: false, CanDelete: false));

        var context = CreateContext(
            requirement: new PermissionRequirement("Citas", PermissionAction.Delete),
            claims: [new Claim("role_id", RoleId.ToString())]);

        await sut.HandleAsync(context);

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
