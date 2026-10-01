using System.Security.Claims;
using Api.Notifications.Hubs;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Xunit;

namespace Api.Tests.Notifications;

public sealed class NotificationsHubTests
{
    [Fact]
    public async Task Connecting_with_a_role_claim_joins_the_role_group()
    {
        var (hub, groups) = CreateHub(role: "Recepcionista", connectionId: "conn-1");

        await hub.OnConnectedAsync();

        await groups.Received(1).AddToGroupAsync(
            "conn-1", NotificationsHubGroups.ReceptionistRole, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Connecting_without_a_role_claim_does_not_join_any_group()
    {
        var (hub, groups) = CreateHub(role: null, connectionId: "conn-2");

        await hub.OnConnectedAsync();

        await groups.DidNotReceive().AddToGroupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static (NotificationsHub Hub, IGroupManager Groups) CreateHub(string? role, string connectionId)
    {
        var claims = new List<Claim>();
        if (role is not null)
        {
            claims.Add(new Claim("role", role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns(connectionId);
        context.User.Returns(principal);

        var groups = Substitute.For<IGroupManager>();

        var hub = new NotificationsHub
        {
            Context = context,
            Groups = groups,
        };

        return (hub, groups);
    }
}
