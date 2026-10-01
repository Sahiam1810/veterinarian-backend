using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Application.Clients.Abstraction;
using Application.Telegram.Abstractions;
using Domain.Clients.Entities;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Infrastructure.Telegram.Security;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Infrastructure.Tests.Telegram;

// Frente 1 (contrato v2, sección 8): el token delegado del bot identifica al cliente con su id.
// U6: person_id se retiró del JWT (era el mismo valor que sub).
public sealed class ClientDelegatedTokenAcceptanceTests
{
    [Fact]
    public async Task Acceptance_Delegated_token_has_sub_equal_to_the_client_id()
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
        var client = new ClientEntity("Ana Cliente", "ana@huellitas.test", "1234567890", "3001234567", null);
        var clients = Substitute.For<IClientRepository>();
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.DelegatedTokenLifetime.Returns(TimeSpan.FromMinutes(5));
        var provider = new AgentDelegatedIdentityProvider(
            clients, settings, new JwtTokenIssuer(options, keys, TimeProvider.System));

        var identity = await provider.GetAsync(client.Id, CancellationToken.None);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(identity.AccessToken);

        var sub = token.Claims.Single(claim => claim.Type == "sub").Value;
        Assert.Equal(client.Id.ToString(), sub);
        Assert.DoesNotContain(token.Claims, claim => claim.Type == "person_id");
        Assert.Equal(client.Id, identity.PersonId);
    }

    private static string Encode(string pem) => Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));
}
