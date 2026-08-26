using Application.Security.Models;
using Infrastructure.Security.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Security.Tokens;

public sealed class JwtTokenIssuer(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider)
{
    private readonly JwtOptions jwtOptions = options.Value;

    public IssuedAccessToken Issue(AuthenticatedIdentity identity)
    {
        var now = timeProvider.GetUtcNow();

        var expiresAt = now.AddMinutes(
            jwtOptions.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                identity.UserAccountId.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

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
                identity.Email),

            new(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                jwtOptions.SigningKey));

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials:
                new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256));

        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}