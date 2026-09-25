using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Application.Clients.Abstraction;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Clients.Entities;
using Domain.Roles;
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
    public async Task Guest_identity_is_stable_isolated_and_contains_the_required_claims()
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
        var clientsRepository = Substitute.For<IClientRepository>();
        var provider = new AgentDelegatedIdentityProvider(
            clientsRepository,
            settings,
            new JwtTokenIssuer(options, keys, TimeProvider.System));

        var first = provider.GetGuest(1001);
        var repeated = provider.GetGuest(1001);
        var different = provider.GetGuest(2002);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(first.AccessToken);

        Assert.Equal(first.PersonId, repeated.PersonId);
        Assert.NotEqual(first.PersonId, different.PersonId);
        Assert.Equal("TelegramGuest", first.Role);
        Assert.Equal(first.PersonId.ToString(), token.Subject);
        Assert.DoesNotContain(token.Claims, x => x.Type == "person_id");
        Assert.True(Guid.TryParse(token.Claims.Single(x => x.Type == "role_id").Value, out _));
        Assert.Equal("TelegramGuest", token.Claims.Single(x => x.Type == "role").Value);
        Assert.Equal("1001", token.Claims.Single(x => x.Type == "telegram_user_id").Value);
        Assert.DoesNotContain(token.Claims, claim => claim.Type == "token_use");

        // Ticket 3 P1: guest no toca CLIENTS ni fabrica teléfono desde GUID/Telegram ID.
        await clientsRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await clientsRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await clientsRepository.DidNotReceiveWithAnyArgs().GetByPhoneAsync(default!, default);
        await clientsRepository.DidNotReceiveWithAnyArgs()
            .ExistsByPhoneAsync(default!, default, default);
        Assert.Equal("guest@telegram.invalid", token.Claims.Single(x => x.Type == "email" || x.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.DoesNotContain(
            token.Claims,
            claim => claim.Type is "phone_number" or "phone" or "phonenumber");
        Assert.DoesNotContain(
            first.PersonId.ToString("N"),
            token.Claims.Single(x => x.Type == "email" || x.Type == JwtRegisteredClaimNames.Email).Value,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Linked_identity_issues_delegated_token_for_client_with_sub_email_and_preferred_username()
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
        var client = new ClientEntity(
            "Cliente Telegram",
            "cliente@telegram.test",
            "1234567890",
            "3001234567",
            "Calle 123");
        var clientsRepository = Substitute.For<IClientRepository>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.DelegatedTokenLifetime.Returns(TimeSpan.FromMinutes(5));
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        var provider = new AgentDelegatedIdentityProvider(
            clientsRepository,
            settings,
            new JwtTokenIssuer(options, keys, TimeProvider.System));

        var identity = await provider.GetAsync(client.Id, CancellationToken.None);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(identity.AccessToken);

        Assert.Equal(client.Id, identity.PersonId);
        Assert.Equal("Cliente", identity.Role);

        // Verification of sub, email, and preferred_username claims
        Assert.Equal(client.Id.ToString(), token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub || claim.Type == "sub").Value);
        Assert.Equal("cliente@telegram.test", token.Claims.Single(claim => claim.Type == "email" || claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("cliente@telegram.test", token.Claims.Single(claim => claim.Type == "preferred_username").Value);

        Assert.DoesNotContain(token.Claims, claim => claim.Type == "person_id");
        Assert.Equal(SystemRoles.ClientRoleId.ToString(), token.Claims.Single(claim => claim.Type == "role_id").Value);
        Assert.Equal("Cliente", token.Claims.Single(claim => claim.Type == "role").Value);
        Assert.Equal(
            "telegram_agent",
            token.Claims.Single(claim => claim.Type == "token_use").Value);
        Assert.DoesNotContain(token.Claims, claim => claim.Type == "telegram_user_id");
    }

    [Fact]
    public async Task Linked_identity_throws_when_client_is_missing_or_inactive()
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
        var clientsRepository = Substitute.For<IClientRepository>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.DelegatedTokenLifetime.Returns(TimeSpan.FromMinutes(5));

        var missingClientId = Guid.NewGuid();
        clientsRepository.GetByIdAsync(missingClientId, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var inactiveClient = new ClientEntity(
            "Cliente Inactivo", "inactivo@test.com", "9999999999", "3000000000", null);
        inactiveClient.Deactivate();
        clientsRepository.GetByIdAsync(inactiveClient.Id, Arg.Any<CancellationToken>())
            .Returns(inactiveClient);

        var provider = new AgentDelegatedIdentityProvider(
            clientsRepository,
            settings,
            new JwtTokenIssuer(options, keys, TimeProvider.System));

        await Assert.ThrowsAsync<TelegramAccountUnavailableException>(() =>
            provider.GetAsync(missingClientId, CancellationToken.None));

        await Assert.ThrowsAsync<TelegramAccountUnavailableException>(() =>
            provider.GetAsync(inactiveClient.Id, CancellationToken.None));
    }

    private static string Encode(string pem) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pem));
}
