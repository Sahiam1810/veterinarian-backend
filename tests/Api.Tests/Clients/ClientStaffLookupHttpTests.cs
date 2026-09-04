using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Clients.Controllers;
using Api.Common.Security.Permissions;
using Api.Tests.Support;
using Application.Clients.UseCases;
using Application.Permissions.UseCases;
using Domain.Clients.Entities;
using Domain.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Clients;

// Contrato HTTP del lookup staff (tarea 2.3): auth + Clientes View + DTO rico.
public sealed class ClientStaffLookupAuthorizationTests
{
    [Fact]
    public void Lookup_requires_Clientes_View_and_is_not_anonymous()
    {
        var method = typeof(ClientsController).GetMethod(nameof(ClientsController.Lookup));
        Assert.NotNull(method);

        var permission = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal($"perm:Clientes:{PermissionAction.View}", permission.Policy);

        Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }
}

public sealed class ClientStaffLookupHttpTests : IClassFixture<ClientStaffLookupApiFactory>
{
    private readonly ClientStaffLookupApiFactory factory;

    public ClientStaffLookupHttpTests(ClientStaffLookupApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Lookup_without_token_returns_401()
    {
        using var client = factory.CreateGuestClient();

        using var response = await client.GetAsync("/api/clients/lookup?identification=1234567890");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Lookup_without_Clientes_View_returns_403()
    {
        using var client = factory.CreateAuthenticatedClient(withClientesView: false);

        using var response = await client.GetAsync("/api/clients/lookup?identification=1234567890");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Lookup_with_Clientes_View_returns_200_rich_dto()
    {
        using var client = factory.CreateAuthenticatedClient(withClientesView: true);

        using var response = await client.GetAsync(
            "/api/clients/lookup?identification=1234567890&phone=3001234567");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;
        Assert.Equal("1234567890", body.GetProperty("identificationNumber").GetString());
        Assert.Equal("3001234567", body.GetProperty("phoneNumber").GetString());
        Assert.True(body.TryGetProperty("address", out _));
        Assert.True(body.TryGetProperty("userId", out _));
    }

    [Fact]
    public async Task Lookup_with_SuperAdmin_returns_200_without_effective_permission_matrix()
    {
        using var client = factory.CreateAuthenticatedClient(
            withClientesView: false,
            roleId: SystemRoles.SuperAdminId);

        using var response = await client.GetAsync("/api/clients/lookup?phone=3001234567");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class ClientStaffLookupApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.ClientLookup.Tests";
    private const string Audience = "Veterinaria.Client.ClientLookup.Tests";
    private const string KeyId = "client-lookup-http-test-key";
    private static readonly Guid StaffRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PersonId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];
    private readonly ISender sender = Substitute.For<ISender>();
    private bool clientesViewGranted = true;

    public ClientStaffLookupApiFactory()
    {
        var environment = new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = Issuer,
            ["Jwt__Audience"] = Audience,
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = KeyId,
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0"
        };

        foreach (var setting in environment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        ConfigureSenderDefaults();
    }

    public HttpClient CreateGuestClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public HttpClient CreateAuthenticatedClient(bool withClientesView, Guid? roleId = null)
    {
        clientesViewGranted = withClientesView;
        var client = CreateGuestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(roleId ?? StaffRoleId));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISender>();
            services.AddSingleton(sender);
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

    private void ConfigureSenderDefaults()
    {
        var clientEntity = new ClientEntity(
            userId: Guid.NewGuid(),
            identificationNumber: "1234567890",
            address: "Calle Operativa 10",
            phoneNumber: "3001234567");

        sender.Send(Arg.Any<GetClientLookupQuery>(), Arg.Any<CancellationToken>())
            .Returns(clientEntity);

        sender.Send(Arg.Any<GetEffectivePermissionQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new EffectivePermission(
                CanView: clientesViewGranted,
                CanCreate: false,
                CanEdit: false,
                CanDelete: false));
    }

    private string CreateToken(Guid roleId)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(
            Convert.FromBase64String(Keys.PrivateKeyPemBase64)));
        var key = new RsaSecurityKey(rsa)
        {
            KeyId = KeyId,
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = false
            }
        };
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim("person_id", PersonId.ToString()),
                new Claim("role_id", roleId.ToString())
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
