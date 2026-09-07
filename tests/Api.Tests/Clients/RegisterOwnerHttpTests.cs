using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Clients.Controllers;
using Api.Clients.Dtos;
using Api.Common.Security.Permissions;
using Api.Tests.Support;
using Application.Common.Exceptions;
using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Owners.UseCases;
using Application.Permissions.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Clients;

// Contrato HTTP de la tarea 4.1: auth + Clientes Create + reutiliza RegisterOwner (sin User/Account/Credentials extra).
public sealed class RegisterOwnerAuthorizationTests
{
    [Fact]
    public void RegisterOwner_requires_Clientes_Create_and_is_not_anonymous()
    {
        var method = typeof(ClientsController).GetMethod(nameof(ClientsController.RegisterOwner));
        Assert.NotNull(method);

        var permission = method.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal($"perm:Clientes:{PermissionAction.Create}", permission.Policy);

        Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void RegisterOwnerDto_never_contains_password_fields()
    {
        var propertyNames = typeof(RegisterOwnerDto).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(
            propertyNames,
            name => name.Contains("password", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class RegisterOwnerHttpTests : IClassFixture<RegisterOwnerApiFactory>
{
    private readonly RegisterOwnerApiFactory factory;

    public RegisterOwnerHttpTests(RegisterOwnerApiFactory factory)
    {
        this.factory = factory;
        // IClassFixture comparte el mismo ISender entre tests: aislar historial y stubs por test.
        factory.Sender.ClearReceivedCalls();
        factory.ResetSenderDefaults();
    }

    private static object ValidBody() => new
    {
        fullName = "María Pérez",
        email = "maria.perez@huellitas.test",
        identificationNumber = "1234567890",
        phoneNumber = "3001234567",
        address = "Calle Falsa 123"
    };

    [Fact]
    public async Task RegisterOwner_without_token_returns_401()
    {
        using var client = factory.CreateGuestClient();

        using var response = await client.PostAsJsonAsync("/api/clients/register-owner", ValidBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterOwner_without_Clientes_Create_returns_403_and_does_not_register_owner()
    {
        using var client = factory.CreateAuthenticatedClient(withClientesCreate: false);

        using var response = await client.PostAsJsonAsync("/api/clients/register-owner", ValidBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await factory.Sender.DidNotReceive().Send(
            Arg.Any<RegisterOwnerCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterOwner_with_Clientes_Create_and_valid_body_returns_201_with_client_dto()
    {
        using var client = factory.CreateAuthenticatedClient(withClientesCreate: true);

        using var response = await client.PostAsJsonAsync("/api/clients/register-owner", ValidBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;
        Assert.True(body.TryGetProperty("id", out _));
        Assert.True(body.TryGetProperty("userId", out _));
        Assert.Equal("1234567890", body.GetProperty("identificationNumber").GetString());
        Assert.False(body.TryGetProperty("password", out _));
        Assert.False(body.TryGetProperty("credentials", out _));
        Assert.False(body.TryGetProperty("accessToken", out _));
    }

    [Fact]
    public async Task RegisterOwner_sends_the_staff_channel_command_with_the_request_data()
    {
        using var client = factory.CreateAuthenticatedClient(withClientesCreate: true);

        using var response = await client.PostAsJsonAsync("/api/clients/register-owner", ValidBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await factory.Sender.Received(1).Send(
            Arg.Is<RegisterOwnerCommand>(c =>
                c.Channel == RegisterOwnerChannel.Staff
                && c.FullName == "María Pérez"
                && c.Email == "maria.perez@huellitas.test"
                && c.IdentificationNumber == "1234567890"
                && c.PhoneNumber == "3001234567"
                && c.Address == "Calle Falsa 123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterOwner_when_the_identification_already_exists_returns_409_with_stable_code()
    {
        factory.Sender.Send(Arg.Any<RegisterOwnerCommand>(), Arg.Any<CancellationToken>())
            .Returns<RegisterOwnerResult>(_ => throw new ConflictException(
                "Ya existe un cliente con ese número de identificación.",
                OwnerRegistrationErrors.IdentificationAlreadyInUse.Code));
        using var client = factory.CreateAuthenticatedClient(withClientesCreate: true);

        using var response = await client.PostAsJsonAsync("/api/clients/register-owner", ValidBody());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            StatusCodes.Status409Conflict,
            document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            OwnerRegistrationErrors.IdentificationAlreadyInUse.Code,
            document.RootElement.GetProperty("error").GetString());
    }
}

public sealed class RegisterOwnerApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.RegisterOwner.Tests";
    private const string Audience = "Veterinaria.Client.RegisterOwner.Tests";
    private const string KeyId = "register-owner-http-test-key";
    private static readonly Guid StaffRoleId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid PersonId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();

    public RegisterOwnerApiFactory()
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

        ResetSenderDefaults();
    }

    public HttpClient CreateGuestClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public HttpClient CreateAuthenticatedClient(bool withClientesCreate, Guid? roleId = null)
    {
        var client = CreateGuestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(roleId ?? StaffRoleId, withClientesCreate));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISender>();
            services.AddSingleton(Sender);
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

    public void ResetSenderDefaults()
    {
        Sender.Send(Arg.Any<RegisterOwnerCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => new RegisterOwnerResult(Guid.NewGuid(), Guid.NewGuid()));

        Sender.Send(Arg.Any<Application.Clients.UseCases.GetClientByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Domain.Clients.Entities.ClientEntity(
                userId: Guid.NewGuid(),
                identificationNumber: "1234567890",
                address: "Calle Falsa 123",
                phoneNumber: "3001234567"));
    }

    // El claim "permissions" (JwtTokenIssuer.BuildToken) es el que evalúa
    // PermissionAuthorizationHandler; lo incrustamos igual que el emisor real.
    private string CreateToken(Guid roleId, bool withClientesCreate)
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

        if (withClientesCreate)
        {
            token.Payload[PermissionClaimValue.ClaimType] = new[]
            {
                PermissionClaimValue.Create("Clientes", "Create")
            };
        }

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
