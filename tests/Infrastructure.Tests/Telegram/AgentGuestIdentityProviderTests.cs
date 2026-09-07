using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Application.Permissions.Claims;
using Application.Permissions.UseCases;
using Application.Roles.Abstraction;
using Application.Telegram.Abstractions;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Infrastructure.Telegram.Security;
using MediatR;
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
        var sender = Substitute.For<ISender>();
        var provider = new AgentDelegatedIdentityProvider(
            Substitute.For<IUsersRepository>(),
            Substitute.For<IUserAccountsRepository>(),
            Substitute.For<IRolesRepository>(),
            sender,
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
        Assert.DoesNotContain(token.Claims, claim => claim.Type == PermissionClaimValue.ClaimType);
        await sender.DidNotReceive().Send(
            Arg.Any<GetUserPermissionClaimsQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Linked_identity_contains_the_current_user_permissions()
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
        var roleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new Domain.Users.Entities.Users(
            "Telegram User",
            "telegram@huellitas.test",
            null,
            roleId);
        var account = new Domain.UserAccounts.Entities.UserAccounts(
            user.Id,
            "telegramuser",
            "telegram@huellitas.test",
            "Activo");
        var role = new Domain.Roles.Entities.Roles("Administrador", "Staff");
        var usersRepository = Substitute.For<IUsersRepository>();
        var accountsRepository = Substitute.For<IUserAccountsRepository>();
        var rolesRepository = Substitute.For<IRolesRepository>();
        var sender = Substitute.For<ISender>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.DelegatedTokenLifetime.Returns(TimeSpan.FromMinutes(5));
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        accountsRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(account);
        rolesRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        sender.Send(
                Arg.Is<GetUserPermissionClaimsQuery>(query =>
                    query.RoleId == roleId && query.UserId == user.Id),
                Arg.Any<CancellationToken>())
            .Returns(["perm:Mascotas:View"]);
        var provider = new AgentDelegatedIdentityProvider(
            usersRepository,
            accountsRepository,
            rolesRepository,
            sender,
            settings,
            new JwtTokenIssuer(options, keys, TimeProvider.System));

        var identity = await provider.GetAsync(user.Id, CancellationToken.None);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(identity.AccessToken);

        Assert.Contains(
            token.Claims,
            claim => claim.Type == PermissionClaimValue.ClaimType &&
                     claim.Value == "perm:Mascotas:View");
    }

    private static string Encode(string pem) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pem));
}
