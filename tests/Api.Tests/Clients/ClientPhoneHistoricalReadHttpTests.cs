using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Domain.Clients.Entities;
using Domain.Roles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Clients;

// GET /api/clients: mezcla válido + histórico inválido (PhoneNumber null en entidad)
// no omite filas ni tumba la lista. La materialización EF se cubre en Infrastructure.
public sealed class ClientPhoneHistoricalReadHttpTests
    : IClassFixture<ClientPhoneHistoricalReadApiFactory>
{
    private readonly ClientPhoneHistoricalReadApiFactory factory;

    public ClientPhoneHistoricalReadHttpTests(ClientPhoneHistoricalReadApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetAll_returns_both_clients_and_null_phone_for_historical_invalid()
    {
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(2, document.RootElement.GetArrayLength());

        var byId = document.RootElement.EnumerateArray()
            .ToDictionary(e => e.GetProperty("id").GetGuid());

        Assert.True(byId.ContainsKey(factory.ValidClient.Id));
        Assert.True(byId.ContainsKey(factory.HistoricalInvalidClient.Id));
        Assert.Equal(
            "3001234567",
            byId[factory.ValidClient.Id].GetProperty("phoneNumber").GetString());
        Assert.Equal(
            JsonValueKind.Null,
            byId[factory.HistoricalInvalidClient.Id].GetProperty("phoneNumber").ValueKind);
    }
}

public sealed class ClientPhoneHistoricalReadApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.ClientPhoneHistoricalRead.Tests";
    private const string Audience = "Veterinaria.Client.ClientPhoneHistoricalRead.Tests";
    private const string KeyId = "client-phone-historical-read-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();

    public ClientEntity ValidClient { get; }
    public ClientEntity HistoricalInvalidClient { get; }

    public ClientPhoneHistoricalReadApiFactory()
    {
        ValidClient = TestClients.Create(
            identificationNumber: "1000000001",
            phoneNumber: "3001234567",
            email: "valido@test.local");
        HistoricalInvalidClient = TestClients.Create(
            identificationNumber: "1000000002",
            phoneNumber: "3009999999",
            email: "historico@test.local");
        typeof(ClientEntity).GetProperty(nameof(ClientEntity.PhoneNumber))!
            .SetValue(HistoricalInvalidClient, null);

        var environment = new Dictionary<string, string>
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
            ["Jwt__ClockSkewSeconds"] = "0"
        };

        foreach (var setting in environment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        unitOfWork.ClientsRepository.Returns(clientsRepository);
        clientsRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ClientEntity>)[ValidClient, HistoricalInvalidClient]);
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateSuperAdminToken());
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton(unitOfWork);
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

    private static string CreateSuperAdminToken()
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
                new Claim("person_id", Guid.NewGuid().ToString()),
                new Claim("role_id", SystemRoles.SuperAdminId.ToString())
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
