// Suite Etapa 1 (seguridad de acceso a plataforma) — tarea 1.5, adaptada T10/U5.
// USERS es solo personal: un rol llamado "Cliente" no tiene trato especial.
// U5: fusionó USER_ACCOUNTS/USER_CREDENTIALS en USERS, así que los casos E/F
// (alta de cuenta/credenciales para un rol llamado Cliente) dejaron de existir
// como pasos separados -- ya no hay handlers de creación de cuenta/credenciales.
//
// Matriz:
// A SuperAdmin OK
// B Staff Admin OK
// C Rol llamado Cliente con creds válidas → emite tokens (sin bloqueo)
// D Email inexistente → Authentication.InvalidCredentials
//
// Run: dotnet test --filter FullyQualifiedName~SecurityStage1

using System.IdentityModel.Tokens.Jwt;
using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Permissions.UseCases;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Domain.Roles;
using Infrastructure.Security;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Security;

public sealed class SecurityStage1Tests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserTokensRepository _userTokenRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUsersRepository _usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRolesRepository _rolesRepository = Substitute.For<IRolesRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();

    private readonly IPasswordHasher _passwordHasher = new PasswordHasher();
    private readonly JwtRsaKeyMaterial _keyMaterial;
    private readonly AuthenticationService _authService;

    public SecurityStage1Tests()
    {
        var keys = RsaTestKeys.Create();
        var jwtOptions = new JwtOptions
        {
            Issuer = "Veterinaria.Api.Tests",
            Audience = "Veterinaria.Client.Tests",
            PrivateKeyPemBase64 = keys.PrivateKeyPemBase64,
            PublicKeyPemBase64 = keys.PublicKeyPemBase64,
            KeyId = "test-key-2026-09",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
            ClockSkewSeconds = 0
        };
        _keyMaterial = new JwtRsaKeyMaterial(Options.Create(jwtOptions));
        var jwtTokenIssuer = new JwtTokenIssuer(
            Options.Create(jwtOptions), _keyMaterial, new FixedTimeProvider(Now));

        _unitOfWork.RolesRepository.Returns(_rolesRepository);
        _unitOfWork.UsersRepository.Returns(_usersRepository);
        _unitOfWork.UserTokensRepository.Returns(_userTokenRepository);

        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(CancellationToken.None));
        _sender.Send(
                Arg.Any<GetUserPermissionClaimsQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        _authService = new AuthenticationService(
            _usersRepository,
            _userTokenRepository,
            _unitOfWork,
            _sender,
            jwtTokenIssuer,
            new RefreshTokenProtector(),
            _passwordHasher,
            Options.Create(jwtOptions),
            new FixedTimeProvider(Now));
    }

    public void Dispose() => _keyMaterial.Dispose();

    // Caso A
    [Fact]
    public async Task Login_SuperAdmin_WithValidCredentials_ReturnsSuccessAndIssuesTokens()
    {
        var role = new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema");
        var user = new UserEntity(
            "Super Administrador",
            SuperAdminEmail,
            _passwordHasher.Hash(SuperAdminPassword),
            SystemRoles.SuperAdminId);
        _usersRepository.GetByEmailAsync(SuperAdminEmail, Arg.Any<CancellationToken>()).Returns(user);
        _usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _authService.LoginAsync(
            SuperAdminEmail, SuperAdminPassword, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal(SystemRoles.SuperAdminId.ToString(), token.Claims.Single(c => c.Type == "role_id").Value);
        Assert.Equal(SystemRoles.SuperAdminName, token.Claims.Single(c => c.Type == "role").Value);
        Assert.DoesNotContain(token.Claims, c => c.Type == "super_admin");
    }

    // Caso B
    [Fact]
    public async Task Login_StaffAdminUser_WithValidAccountAndCredentials_ReturnsSuccessAndIssuesTokens()
    {
        var adminRole = new RoleEntity("Administrador", "Administrador del sistema");
        var rawPassword = "StaffPassword123!";
        var adminUser = new UserEntity(
            "Admin Staff", "admin.staff@huellitas.test", _passwordHasher.Hash(rawPassword), adminRole.Id);

        _usersRepository.GetByEmailAsync(adminUser.Email.Value, Arg.Any<CancellationToken>())
            .Returns(adminUser);
        _usersRepository.GetByIdAsync(adminUser.Id, Arg.Any<CancellationToken>())
            .Returns(adminUser);
        _rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>())
            .Returns(adminRole);

        var result = await _authService.LoginAsync(
            "admin.staff@huellitas.test", rawPassword, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal(adminRole.Name.Value, token.Claims.Single(c => c.Type == "role").Value);
    }

    // Caso C — un rol llamado "Cliente" no bloquea login de plataforma.
    [Fact]
    public async Task Login_RoleNamedCliente_WithValidPassword_IssuesTokens()
    {
        var clientRole = new RoleEntity("Cliente", "Nombre de rol sin trato especial");
        var rawPassword = "ClientPassword123!";
        var clientUser = new UserEntity(
            "Cliente Test", "cliente.login@huellitas.test", _passwordHasher.Hash(rawPassword), clientRole.Id);

        _usersRepository.GetByEmailAsync(clientUser.Email.Value, Arg.Any<CancellationToken>())
            .Returns(clientUser);
        _usersRepository.GetByIdAsync(clientUser.Id, Arg.Any<CancellationToken>())
            .Returns(clientUser);
        _rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>())
            .Returns(clientRole);

        var result = await _authService.LoginAsync(
            "cliente.login@huellitas.test", rawPassword, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
    }

    // Caso D — anti-enumeración (mismo code que credenciales inválidas).
    [Fact]
    public async Task Login_NonExistentEmail_ReturnsInvalidCredentials()
    {
        var nonExistentEmail = "noexiste@huellitas.test";
        _usersRepository.GetByEmailAsync(nonExistentEmail, Arg.Any<CancellationToken>())
            .Returns((UserEntity?)null);

        var result = await _authService.LoginAsync(
            nonExistentEmail, "AnyPassword123!", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials.Code, result.Error.Code);
    }

    private const string SuperAdminEmail = "superadmin@huellitas.test";
    private const string SuperAdminPassword = "SuperAdminPassword123!";

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
