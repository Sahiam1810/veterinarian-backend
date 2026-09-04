using Application.Security.Models;
using Infrastructure.Security.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Security.Tokens;

public sealed class JwtTokenIssuer(
    IOptions<JwtOptions> options,
    JwtRsaKeyMaterial keyMaterial,
    TimeProvider timeProvider)
{
    private readonly JwtOptions jwtOptions = options.Value;

    public IssuedAccessToken Issue(AuthenticatedIdentity identity) =>
        Issue(identity, TimeSpan.FromMinutes(jwtOptions.AccessTokenMinutes));

    public IssuedAccessToken Issue(AuthenticatedIdentity identity, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                identity.UserAccountId.ToString()),

            new(
                "person_id",
                identity.PersonId.ToString()),

            new(
                "role_id",
                identity.RoleId.ToString()),

            new(
                "role",
                identity.Role),

            new(
                "preferred_username",
                identity.UserName),

            new(
                JwtRegisteredClaimNames.Email,
                identity.Email)
        };

        return BuildToken(claims, lifetime);
    }

    private IssuedAccessToken BuildToken(List<Claim> claims, TimeSpan lifetime)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(lifetime);

        claims.Add(new(
            JwtRegisteredClaimNames.Jti,
            Guid.NewGuid().ToString()));
        claims.Add(new(
            JwtRegisteredClaimNames.Iat,
            now.ToUnixTimeSeconds().ToString(),
            ClaimValueTypes.Integer64));

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials:
                new SigningCredentials(
                    keyMaterial.SigningKey,
                    SecurityAlgorithms.RsaSha256));

        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
