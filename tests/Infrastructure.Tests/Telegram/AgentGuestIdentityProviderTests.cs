using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Application.Roles.Abstraction;
using Application.Telegram.Abstractions;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Infrastructure.Telegram.Security;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class AgentGuestIdentityProviderTests
{
    [Fact]
    public void Guest_identity_is_stable_isolated_and_contains_the_required_claims()
    {
        using var rsa = RSA.Create(2048);
        var options = Options.Create(new JwtOptions
        {
            Issuer = "Veterinaria.Api",
            Audience = "Veterinaria.Client",
            PrivateKeyPemBase64 = Encode(rsa.ExportPkcs8PrivateKeyPem()),
            PublicKeyPemBase64 = Encode(rsa.ExportSubjectPublicKeyInfoPem()),
            KeyId = "test-key"
        });
        using var keys = new JwtRsaKeyMaterial(options);
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.DelegatedTokenLifetime.Returns(TimeSpan.FromMinutes(5));
        var provider = new AgentDelegatedIdentityProvider(
            Substitute.For<IUsersRepository>(),
            Substitute.For<IUserAccountsRepository>(),
            Substitute.For<IRolesRepository>(),
            settings,
            new JwtTokenIssuer(options, keys, TimeProvider.System));

        var first = provider.GetGuest(1001);
        var repeated = provider.GetGuest(1001);
        var different = provider.GetGuest(2002);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(first.AccessToken);

        Assert.Equal(first.PersonId, repeated.PersonId);
        Assert.NotEqual(first.PersonId, different.PersonId);
        Assert.Equal("TelegramGuest", first.Role);
        Assert.Equal(first.PersonId.ToString(), token.Claims.Single(x => x.Type == "person_id").Value);
        Assert.True(Guid.TryParse(token.Claims.Single(x => x.Type == "role_id").Value, out _));
        Assert.Equal("TelegramGuest", token.Claims.Single(x => x.Type == "role").Value);
    }

    private static string Encode(string pem) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pem));
}
