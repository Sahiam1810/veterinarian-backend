using Application.Permissions.Claims;
using Application.Permissions.UseCases;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Permissions;

public sealed class GetUserPermissionClaimsQueryHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task Handle_emits_only_granted_actions_in_deterministic_order()
    {
        sender.Send(
                Arg.Is<GetUserEffectivePermissionsQuery>(query =>
                    query.RoleId == RoleId && query.UserId == UserId),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, EffectivePermission>
            {
                ["Mascotas"] = new(true, true, false, false),
                ["Citas"] = new(true, false, true, false),
                ["Usuarios"] = EffectivePermission.None
            });
        var sut = new GetUserPermissionClaimsQueryHandler(sender);

        var result = await sut.Handle(
            new GetUserPermissionClaimsQuery(RoleId, UserId),
            CancellationToken.None);

        Assert.Equal(
            [
                "perm:Citas:Edit",
                "perm:Citas:View",
                "perm:Mascotas:Create",
                "perm:Mascotas:View"
            ],
            result);
    }

    [Theory]
    [InlineData("perm:Mascotas:Create", "Mascotas", "Create")]
    [InlineData("perm:Historiales Clínicos:View", "Historiales Clínicos", "View")]
    public void TryParse_reads_an_exact_permission_value(
        string value,
        string expectedModule,
        string expectedAction)
    {
        var parsed = PermissionClaimValue.TryParse(value, out var moduleName, out var action);

        Assert.True(parsed);
        Assert.Equal(expectedModule, moduleName);
        Assert.Equal(expectedAction, action);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Mascotas:View")]
    [InlineData("perm::View")]
    [InlineData("perm:Mascotas:")]
    public void TryParse_rejects_malformed_permission_values(string value)
    {
        var parsed = PermissionClaimValue.TryParse(value, out _, out _);

        Assert.False(parsed);
    }
}
