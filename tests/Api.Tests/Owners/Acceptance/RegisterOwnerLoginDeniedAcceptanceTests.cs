using Api.Tests.Support;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.UseCases;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Application.Verification.Abstractions;
using Domain.Clients.Entities;
using Domain.ContactVerification.Enums;
using Infrastructure.Security;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Owners.Acceptance;

// Tarea 4.5: login denegado tras RegisterOwner (fakes). Sin SMTP ni HTTP 4.1–4.4.
public sealed class RegisterOwnerLoginDeniedAcceptanceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 15, 0, 0, TimeSpan.Zero);
    private const string Email = "ana.owner@huellitas.test";
    private const string PasswordAttempt = "AnyPassword123!";

    private readonly JwtRsaKeyMaterial keyMaterial;

    public RegisterOwnerLoginDeniedAcceptanceTests()
    {
        var keys = RsaTestKeys.Create();
        var jwtOptions = new JwtOptions
        {
            Issuer = "Veterinaria.Api.Stage4.Tests",
            Audience = "Veterinaria.Client.Stage4.Tests",
            PrivateKeyPemBase64 = keys.PrivateKeyPemBase64,
            PublicKeyPemBase64 = keys.PublicKeyPemBase64,
            KeyId = "stage4-login-denied-key",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
            ClockSkewSeconds = 0
        };
        keyMaterial = new JwtRsaKeyMaterial(Options.Create(jwtOptions));
        JwtOptions = jwtOptions;
        Issuer = new JwtTokenIssuer(
            Options.Create(jwtOptions), keyMaterial, new FixedClock(Now));
    }

    private JwtOptions JwtOptions { get; }
    private JwtTokenIssuer Issuer { get; }

    public void Dispose() => keyMaterial.Dispose();

    [Fact]
    public async Task Acceptance_RegisteredEmail_LoginWithoutAccount_DoesNotIssueJwt()
    {
        var registered = await RegisterOwnerAsync();
        var accounts = Substitute.For<IUserAccountsRepository>();
        accounts.GetByMailAsync(Email, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        var auth = CreateAuth(accounts, Substitute.For<IUserCredentialsRepository>(), registered.Users);

        var result = await auth.LoginAsync(Email, PasswordAttempt, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthenticationErrors.InvalidCredentials.Code, result.Error.Code);
        Assert.Null(registered.User.PasswordHash);
    }

    [Fact]
    public async Task Acceptance_ClienteWithLegacyLogin_IsPlatformAccessDenied()
    {
        var registered = await RegisterOwnerAsync();
        var hasher = new PasswordHasher();
        var account = new UserAccountEntity(registered.User.Id, "ana.owner", Email, "Activo");
        var credentials = new UserCredentialsEntity(account.Id, hasher.Hash(PasswordAttempt));

        var accounts = Substitute.For<IUserAccountsRepository>();
        var creds = Substitute.For<IUserCredentialsRepository>();
        accounts.GetByMailAsync(Email, Arg.Any<CancellationToken>()).Returns(account);
        creds.GetByAccountIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(credentials);

        var auth = CreateAuth(accounts, creds, registered.Users, registered.Roles);

        var result = await auth.LoginAsync(Email, PasswordAttempt, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, result.Error.Code);
    }

    private AuthenticationService CreateAuth(
        IUserAccountsRepository accounts,
        IUserCredentialsRepository credentials,
        IUsersRepository users,
        IRolesRepository? roles = null)
    {
        var tokens = Substitute.For<IUserTokensRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sender = Substitute.For<ISender>();
        unitOfWork.UsersRepository.Returns(users);
        if (roles is not null)
        {
            unitOfWork.RolesRepository.Returns(roles);
        }

        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(CancellationToken.None));
        sender.Send(Arg.Any<Application.Permissions.UseCases.GetUserPermissionClaimsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        return new AuthenticationService(
            accounts,
            credentials,
            tokens,
            users,
            unitOfWork,
            sender,
            Issuer,
            new RefreshTokenProtector(),
            new PasswordHasher(),
            Options.Create(JwtOptions),
            new FixedClock(Now));
    }

    private static async Task<RegisteredOwner> RegisterOwnerAsync()
    {
        var clientRole = new RoleEntity("Cliente", "Dueño");
        var users = Substitute.For<IUsersRepository>();
        var clients = Substitute.For<IClientRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var credentials = Substitute.For<IUserCredentialsRepository>();
        var consumeProof = Substitute.For<IConsumeContactVerificationProof>();
        var otp = Substitute.For<IOtpProtector>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        UserEntity? created = null;

        unitOfWork.UsersRepository.Returns(users);
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.RolesRepository.Returns(roles);
        unitOfWork.UserAccountsRepository.Returns(accounts);
        unitOfWork.UserCredentialsRepository.Returns(credentials);
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        roles.GetByNameAsync("Cliente", Arg.Any<CancellationToken>()).Returns(clientRole);
        roles.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>()).Returns(clientRole);
        users.ExistsByEmailAsync(Email, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(false);
        clients.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clients.ExistsByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(false);
        users.AddAsync(Arg.Do<UserEntity>(user => created = user), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => created is not null && call.Arg<Guid>() == created.Id ? created : null);

        var settings = Substitute.For<IRegisterOwnerSettings>();
        settings.RequiresContactProof(RegisterOwnerChannel.Staff).Returns(false);

        var handler = new RegisterOwnerCommandHandler(unitOfWork, consumeProof, settings, otp);
        await handler.Handle(
            new RegisterOwnerCommand("Ana Dueña", Email, "1234567890", "3001234567", RegisterOwnerChannel.Staff),
            CancellationToken.None);

        return new RegisteredOwner(created!, users, roles);
    }

    private sealed record RegisteredOwner(UserEntity User, IUsersRepository Users, IRolesRepository Roles);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
