// Run: dotnet test --filter FullyQualifiedName~SecurityStage1

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.UserAccounts.Abstraction;
using Application.UserAccounts.UseCase;
using Application.UserCredentials.Abstraction;
using Application.UserCredentials.UseCase;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;
using Infrastructure.Security;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

public sealed class SecurityStage1Tests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SuperAdminId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private const string SuperAdminEmail = "superadmin@huellitas.test";
    private const string SuperAdminPassword = "SuperAdminPassword123!";

    private readonly IUserAccountsRepository _userAccountRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository _userCredentialRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUserTokensRepository _userTokenRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUsersRepository _usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRolesRepository _rolesRepository = Substitute.For<IRolesRepository>();

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

        var superAdminOptions = new SuperAdminOptions
        {
            Enabled = true,
            Id = SuperAdminId,
            Email = SuperAdminEmail,
            PasswordHash = _passwordHasher.Hash(SuperAdminPassword)
        };

        _unitOfWork.RolesRepository.Returns(_rolesRepository);
        _unitOfWork.UsersRepository.Returns(_usersRepository);
        _unitOfWork.UserAccountsRepository.Returns(_userAccountRepository);
        _unitOfWork.UserCredentialsRepository.Returns(_userCredentialRepository);
        _unitOfWork.UserTokensRepository.Returns(_userTokenRepository);

        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(CancellationToken.None));

        _authService = new AuthenticationService(
            _userAccountRepository,
            _userCredentialRepository,
            _userTokenRepository,
            _usersRepository,
            _unitOfWork,
            jwtTokenIssuer,
            new RefreshTokenProtector(),
            _passwordHasher,
            Options.Create(jwtOptions),
            Options.Create(superAdminOptions),
            new FixedTimeProvider(Now));
    }

    public void Dispose() => _keyMaterial.Dispose();

    // Caso A (SuperAdmin OK): Hacer login con credenciales válidas configuradas para un SuperAdmin. Resultado esperado: Success (Tokens generados).
    [Fact]
    public async Task Login_SuperAdmin_WithValidCredentials_ReturnsSuccessAndIssuesTokens()
    {
        // Arrange
        var email = SuperAdminEmail;
        var password = SuperAdminPassword;

        // Act
        var result = await _authService.LoginAsync(email, password, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, "SuperAdmin con credenciales válidas debe autenticarse exitosamente.");
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal("true", token.Claims.Single(c => c.Type == "super_admin").Value);
    }

    // Caso B (Staff Admin OK): Crear en el fixture un User rol Administrador + account + credentials válidos. Resultado esperado: Success.
    [Fact]
    public async Task Login_StaffAdminUser_WithValidAccountAndCredentials_ReturnsSuccessAndIssuesTokens()
    {
        // Arrange
        var adminRole = new RoleEntity("Administrador", "Administrador del sistema");
        var adminUser = new UserEntity("Admin Staff", "admin.staff@huellitas.test", null, adminRole.Id);
        var adminAccount = new UserAccountEntity(adminUser.Id, "adminstaff", "admin.staff@huellitas.test", "Activo");
        var rawPassword = "StaffPassword123!";
        var passwordHash = _passwordHasher.Hash(rawPassword);
        var adminCredentials = new UserCredentialEntity(adminAccount.Id, passwordHash);

        _userAccountRepository.GetByMailAsync(adminAccount.Mail.Value, Arg.Any<CancellationToken>())
            .Returns(adminAccount);
        _userCredentialRepository.GetByAccountIdAsync(adminAccount.Id, Arg.Any<CancellationToken>())
            .Returns(adminCredentials);
        _usersRepository.GetByIdAsync(adminUser.Id, Arg.Any<CancellationToken>())
            .Returns(adminUser);
        _rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>())
            .Returns(adminRole);

        // Act
        var result = await _authService.LoginAsync("admin.staff@huellitas.test", rawPassword, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, "Staff Administrador con credenciales válidas debe autenticarse exitosamente.");
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal(adminRole.Name.Value, token.Claims.Single(c => c.Type == "role").Value);
    }

    // Caso C (Cliente denegado): Crear en el fixture un User rol "Cliente" + account + credentials válidos (simulando un dato legacy o bypass). Intentar hacer login. Resultado esperado: Failure. Assertar código de plataforma denegada.
    [Fact]
    public async Task Login_ClientRole_EvenWithValidPassword_ReturnsPlatformAccessDenied()
    {
        // Arrange
        var clientRole = new RoleEntity("Cliente", "Cliente de la veterinaria");
        var clientUser = new UserEntity("Cliente Test", "cliente.login@huellitas.test", null, clientRole.Id);
        var clientAccount = new UserAccountEntity(clientUser.Id, "clientelogin", "cliente.login@huellitas.test", "Activo");
        var rawPassword = "ClientPassword123!";
        var passwordHash = _passwordHasher.Hash(rawPassword);
        var clientCredentials = new UserCredentialEntity(clientAccount.Id, passwordHash);

        _userAccountRepository.GetByMailAsync(clientAccount.Mail.Value, Arg.Any<CancellationToken>())
            .Returns(clientAccount);
        _userCredentialRepository.GetByAccountIdAsync(clientAccount.Id, Arg.Any<CancellationToken>())
            .Returns(clientCredentials);
        _usersRepository.GetByIdAsync(clientUser.Id, Arg.Any<CancellationToken>())
            .Returns(clientUser);
        _rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>())
            .Returns(clientRole);

        // Act
        var result = await _authService.LoginAsync("cliente.login@huellitas.test", rawPassword, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure, "El rol Cliente no debe tener acceso a login tradicional de plataforma.");
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, result.Error.Code);
    }

    // Caso D (Anti-enumeración): Intentar hacer login con un correo que no existe. Resultado esperado: Failure. Assertar código acordado (InvalidCredentials).
    [Fact]
    public async Task Login_NonExistentEmail_ReturnsInvalidCredentials()
    {
        // Arrange
        var nonExistentEmail = "noexiste@huellitas.test";
        _userAccountRepository.GetByMailAsync(nonExistentEmail, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        // Act
        var result = await _authService.LoginAsync(nonExistentEmail, "AnyPassword123!", CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure, "El intento de login con email no existente debe fallar.");
        Assert.Equal(AuthenticationErrors.InvalidCredentials.Code, result.Error.Code);
    }

    // Caso E (Bloqueo de Account para Cliente): Tomar un User rol Cliente existente e intentar ejecutar el comando de crear UserAccount. Resultado esperado: Failure.
    [Fact]
    public async Task CreateUserAccount_WhenUserHasClientRole_FailsOrThrowsForbidden()
    {
        // Arrange
        var clientRole = new RoleEntity("Cliente", "Rol Cliente");
        var clientUser = new UserEntity("Cliente Dummy", "cliente.account@huellitas.test", null, clientRole.Id);

        _usersRepository.GetByIdAsync(clientUser.Id, Arg.Any<CancellationToken>()).Returns(clientUser);
        _rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>()).Returns(clientRole);

        var handler = new CreateUserAccountCommandHandler(_unitOfWork);
        var command = new CreateUserAccountCommand(clientUser.Id, "clienteuser", "cliente.account@huellitas.test", "Activo");

        // Act & Assert
        // Debe fallar al intentar crear una cuenta para un usuario con rol Cliente (regla de no-login para clientes)
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        Assert.NotNull(exception);
    }

    // Caso F (Bloqueo de Credentials para Cliente): Tomar un Account ligado a un Cliente (fabricado en test) e intentar ejecutar el comando de crear UserCredentials. Resultado esperado: Failure.
    [Fact]
    public async Task CreateUserCredentials_WhenAccountBelongsToClient_FailsOrThrowsForbidden()
    {
        // Arrange
        var clientRole = new RoleEntity("Cliente", "Rol Cliente");
        var clientUser = new UserEntity("Cliente Dummy", "cliente.creds@huellitas.test", null, clientRole.Id);
        var clientAccount = new UserAccountEntity(clientUser.Id, "clientecreds", "cliente.creds@huellitas.test", "Activo");

        _userAccountRepository.GetByIdAsync(clientAccount.Id, Arg.Any<CancellationToken>()).Returns(clientAccount);
        _usersRepository.GetByIdAsync(clientUser.Id, Arg.Any<CancellationToken>()).Returns(clientUser);
        _rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>()).Returns(clientRole);

        var handler = new CreateUserCredentialsCommandHandler(_unitOfWork, _passwordHasher);
        var command = new CreateUserCredentialsCommand(clientAccount.Id, "Password123!");

        // Act & Assert
        // Debe fallar al intentar crear credenciales para una cuenta perteneciente a un rol Cliente
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        Assert.NotNull(exception);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
