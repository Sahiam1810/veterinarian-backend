using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Tests.Support;
using Application.Security.Models;
using Domain.Roles;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Api.Tests.Security;

public sealed class JwtTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(
        2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_creates_rs256_token_with_key_id_and_existing_claims()
    {
        var options = CreateOptions(RsaTestKeys.Create());
        using var keyMaterial = new JwtRsaKeyMaterial(Options.Create(options));
        var issuer = new JwtTokenIssuer(
            Options.Create(options),
            keyMaterial,
            new FixedTimeProvider(Now));

        var issued = issuer.Issue(CreateIdentity());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);

        Assert.Equal(SecurityAlgorithms.RsaSha256, token.Header.Alg);
        Assert.Equal(options.KeyId, token.Header.Kid);
        Assert.Equal("11111111-1111-1111-1111-111111111111", token.Subject);
        Assert.Equal("Veterinario", Claim(token, "role"));
        Assert.Equal("22222222-2222-2222-2222-222222222222", Claim(token, "person_id"));
        Assert.Equal("33333333-3333-3333-3333-333333333333", Claim(token, "role_id"));
        Assert.Equal("ana.vet", Claim(token, "preferred_username"));
        Assert.Equal("ana@huellitas.test", Claim(token, JwtRegisteredClaimNames.Email));
        Assert.Equal(Now.AddMinutes(15), issued.ExpiresAt);
    }

    [Fact]
    public void Issue_creates_token_validated_by_the_matching_public_key()
    {
        var options = CreateOptions(RsaTestKeys.Create());
        using var keyMaterial = new JwtRsaKeyMaterial(Options.Create(options));
        var issuer = new JwtTokenIssuer(
            Options.Create(options),
            keyMaterial,
            new FixedTimeProvider(Now));
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        var issued = issuer.Issue(CreateIdentity());
        var principal = handler.ValidateToken(
            issued.Token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = keyMaterial.ValidationKey,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidateLifetime = false
            },
            out _);

        Assert.Equal(
            "11111111-1111-1111-1111-111111111111",
            principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
    }

    [Fact]
    public void Issue_for_persisted_SuperAdmin_creates_normal_role_claims()
    {
        var options = CreateOptions(RsaTestKeys.Create());
        using var keyMaterial = new JwtRsaKeyMaterial(Options.Create(options));
        var issuer = new JwtTokenIssuer(
            Options.Create(options),
            keyMaterial,
            new FixedTimeProvider(Now));

        var identity = new AuthenticatedIdentity(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            SystemRoles.SuperAdminId,
            SystemRoles.SuperAdminName,
            "Super Administrador",
            "superadmin",
            "superadmin@huellitas.test",
            "Activo");
        var issued = issuer.Issue(identity);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);

        Assert.Equal(SecurityAlgorithms.RsaSha256, token.Header.Alg);
        Assert.Equal(identity.UserAccountId.ToString(), token.Subject);
        Assert.Equal(SystemRoles.SuperAdminId.ToString(), Claim(token, "role_id"));
        Assert.Equal(SystemRoles.SuperAdminName, Claim(token, "role"));
        Assert.Equal("superadmin@huellitas.test", Claim(token, JwtRegisteredClaimNames.Email));
        Assert.DoesNotContain(token.Claims, claim => claim.Type == "super_admin");
        Assert.Equal(Now.AddMinutes(15), issued.ExpiresAt);
    }

    private static string Claim(JwtSecurityToken token, string type) =>
        token.Claims.Single(claim => claim.Type == type).Value;

    private static JwtOptions CreateOptions(RsaTestKeys keys) => new()
    {
        Issuer = "Veterinaria.Api.Tests",
        Audience = "Veterinaria.Client.Tests",
        PrivateKeyPemBase64 = keys.PrivateKeyPemBase64,
        PublicKeyPemBase64 = keys.PublicKeyPemBase64,
        KeyId = "test-key-2026-08",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
        ClockSkewSeconds = 0
    };

    private static AuthenticatedIdentity CreateIdentity() => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        "Veterinario",
        "Ana Veterinaria",
        "ana.vet",
        "ana@huellitas.test",
        "Activo");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
