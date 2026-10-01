using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Permissions.UseCases;
using Application.Roles.Abstraction;
using Application.Security.Abstractions;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.Security;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Tokens;
using Domain.Roles;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Api.Tests.Security;

internal static class AuthSecurityTestUsers
{
    public const string StaffAEmail = "staff-a@huellitas.test";
    public const string StaffAPassword = "StaffPasswordA123!";
    public const string StaffBEmail = "staff-b@huellitas.test";
    public const string StaffBPassword = "StaffPasswordB123!";
    public const string ClienteNoHashEmail = "cliente.nohash@huellitas.test";
    public const string ClientePlausiblePassword = "ClientePlausible123!";
    public const string ClienteRandomPassword = "xK9#mQ2!random";
    public const string SuperAdminEmail = "superadmin@huellitas.test";
    public const string SuperAdminPassword = "SuperAdminPassword123!";
}

public sealed class AuthSecurityHttpApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.AuthSecurity.Tests";
    private const string Audience = "Veterinaria.Client.AuthSecurity.Tests";
    private const string KeyId = "auth-security-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private static readonly IReadOnlyDictionary<string, string> TestEnvironment =
        new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Chat__ResolvedEscalationStatusId"] = "85000000-0000-0000-0000-000000000004",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = Issuer,
            ["Jwt__Audience"] = Audience,
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = KeyId,
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__LoginPermitLimit"] = "1000",
            ["RateLimiting__LoginWindowSeconds"] = "60",
            ["RateLimiting__RefreshPermitLimit"] = "1000",
            ["RateLimiting__RefreshWindowSeconds"] = "60",
            ["RateLimiting__TelegramWebhookPermitLimit"] = "1000",
            ["RateLimiting__TelegramWebhookWindowSeconds"] = "60"
        };

    private readonly Dictionary<string, string?> originalEnvironment = [];
    private readonly List<UserTokenEntity> refreshTokens = [];
    private readonly IPasswordHasher passwordHasher = new PasswordHasher();

    // U7: expuestos para los tests de aceptación (alta de usuario con el
    // mismo rol "Administrador" que staffA/staffB, y desactivación directa
    // de un usuario ya logueado sin pasar por el endpoint HTTP).
    private readonly Dictionary<Guid, UserEntity> usersById = [];
    private readonly Dictionary<string, UserEntity> usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Guid StaffRoleId { get; private set; }

    public void DeactivateUser(string email)
    {
        if (usersByEmail.TryGetValue(email.Trim().ToLowerInvariant(), out var user))
        {
            user.Deactivate();
        }
    }

    public AuthSecurityHttpApiFactory()
    {
        foreach (var setting in TestEnvironment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            usersById.Clear();
            usersByEmail.Clear();

            var staffRole = new RoleEntity("Administrador", "Staff panel");
            StaffRoleId = staffRole.Id;
            var clientRole = new RoleEntity("Cliente", "Cliente sin panel");
            // El Id real de SuperAdmin lo fija SystemRoles.SuperAdminId, no el
            // Id que genera el constructor de RoleEntity -- se registra en el
            // diccionario bajo esa clave explícita, igual que en
            // AuthenticationServiceSuperAdminTests.
            var superAdminRole = new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema");

            // U5: USERS ya trae la contraseña directamente -- no hay cuenta
            // ni credenciales separadas que fusionar.
            var staffAUser = new UserEntity(
                "Staff A",
                AuthSecurityTestUsers.StaffAEmail,
                passwordHasher.Hash(AuthSecurityTestUsers.StaffAPassword),
                staffRole.Id);
            var staffBUser = new UserEntity(
                "Staff B",
                AuthSecurityTestUsers.StaffBEmail,
                passwordHasher.Hash(AuthSecurityTestUsers.StaffBPassword),
                staffRole.Id);
            // Hash vacío: simula un usuario sin contraseña utilizable (nunca
            // debe poder loguearse, sea cual sea la contraseña enviada).
            var clientUser = new UserEntity(
                "Cliente Sin Hash",
                AuthSecurityTestUsers.ClienteNoHashEmail,
                string.Empty,
                clientRole.Id);
            // U7: para los tests de aceptación (alta de usuario + login, sin
            // excepciones de permiso por usuario).
            var superAdminUser = new UserEntity(
                "Super Admin",
                AuthSecurityTestUsers.SuperAdminEmail,
                passwordHasher.Hash(AuthSecurityTestUsers.SuperAdminPassword),
                SystemRoles.SuperAdminId);

            foreach (var user in new[] { staffAUser, staffBUser, clientUser, superAdminUser })
            {
                usersById[user.Id] = user;
                usersByEmail[user.Email.Value] = user;
            }

            var rolesById = new Dictionary<Guid, RoleEntity>
            {
                [staffRole.Id] = staffRole,
                [clientRole.Id] = clientRole,
                [SystemRoles.SuperAdminId] = superAdminRole
            };

            var usersRepository = Substitute.For<IUsersRepository>();
            usersRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var email = call.Arg<string>().Trim().ToLowerInvariant();
                    usersByEmail.TryGetValue(email, out var user);
                    return Task.FromResult<UserEntity?>(user);
                });
            usersRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    usersById.TryGetValue(call.Arg<Guid>(), out var user);
                    return Task.FromResult<UserEntity?>(user);
                });
            usersRepository.ExistsByEmailAsync(
                    Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
                .Returns(call =>
                {
                    var email = call.Arg<string>().Trim().ToLowerInvariant();
                    return Task.FromResult(usersByEmail.ContainsKey(email));
                });
            usersRepository.AddAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var user = call.Arg<UserEntity>();
                    usersById[user.Id] = user;
                    usersByEmail[user.Email.Value] = user;
                    return Task.CompletedTask;
                });
            usersRepository.UpdateAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var rolesRepository = Substitute.For<IRolesRepository>();
            rolesRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    rolesById.TryGetValue(call.Arg<Guid>(), out var role);
                    return Task.FromResult<RoleEntity?>(role);
                });

            var userTokensRepository = Substitute.For<IUserTokensRepository>();
            userTokensRepository.AddAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    refreshTokens.Add(call.Arg<UserTokenEntity>());
                    return Task.CompletedTask;
                });
            userTokensRepository.GetByTokenValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var match = refreshTokens.FirstOrDefault(
                        token => token.TokenValue == call.Arg<string>());
                    return Task.FromResult<UserTokenEntity?>(match);
                });
            userTokensRepository.GetAllByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    IReadOnlyCollection<UserTokenEntity> tokens = refreshTokens
                        .Where(token => token.UserId == call.Arg<Guid>())
                        .ToArray();
                    return Task.FromResult(tokens);
                });
            userTokensRepository.DeleteAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    refreshTokens.Remove(call.Arg<UserTokenEntity>());
                    return Task.CompletedTask;
                });

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.UsersRepository.Returns(usersRepository);
            unitOfWork.RolesRepository.Returns(rolesRepository);
            unitOfWork.ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(CancellationToken.None));
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0));

            var permissionSender = Substitute.For<ISender>();
            permissionSender.Send(Arg.Any<GetUserPermissionClaimsQuery>(), Arg.Any<CancellationToken>())
                .Returns(Array.Empty<string>());

            services.RemoveAll<IUserTokensRepository>();
            services.RemoveAll<IUsersRepository>();
            services.RemoveAll<IRolesRepository>();
            services.RemoveAll<IUnitOfWork>();
            services.RemoveAll<IAuthenticationService>();

            services.AddSingleton(userTokensRepository);
            services.AddSingleton(usersRepository);
            services.AddSingleton(rolesRepository);
            services.AddSingleton(unitOfWork);
            services.AddSingleton<IAuthenticationService>(sp => new AuthenticationService(
                usersRepository,
                userTokensRepository,
                unitOfWork,
                permissionSender,
                sp.GetRequiredService<JwtTokenIssuer>(),
                sp.GetRequiredService<RefreshTokenProtector>(),
                passwordHasher,
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Infrastructure.Security.Options.JwtOptions>>(),
                sp.GetRequiredService<TimeProvider>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            foreach (var setting in originalEnvironment)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }
        }
    }
}

[CollectionDefinition("AuthSecurityHttp", DisableParallelization = true)]
public sealed class AuthSecurityHttpCollection : ICollectionFixture<AuthSecurityHttpApiFactory>;
