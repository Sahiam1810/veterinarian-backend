using System.Security.Claims;
using Api.Common.Security;
using Xunit;

namespace Api.Tests.Security;

public sealed class ClaimsPrincipalTryGetClientIdTests
{
    private static readonly Guid ClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Reads_the_client_id_from_the_sub_claim()
    {
        var principal = PrincipalWith(new Claim("sub", ClientId.ToString()));

        var found = principal.TryGetClientId(out var clientId);

        Assert.True(found);
        Assert.Equal(ClientId, clientId);
    }

    [Fact]
    public void Reads_the_client_id_from_the_name_identifier_claim()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, ClientId.ToString()));

        var found = principal.TryGetClientId(out var clientId);

        Assert.True(found);
        Assert.Equal(ClientId, clientId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Rejects_an_empty_or_invalid_subject(string subject)
    {
        var principal = PrincipalWith(new Claim("sub", subject));

        var found = principal.TryGetClientId(out _);

        Assert.False(found);
    }

    [Fact]
    public void Rejects_a_token_without_subject()
    {
        var principal = PrincipalWith(new Claim("person_id", ClientId.ToString()));

        var found = principal.TryGetClientId(out _);

        Assert.False(found);
    }

    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));
}
