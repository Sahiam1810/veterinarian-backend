using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Services.UseCases;
using Domain.Services.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Services;

public sealed class AvailableServicesHttpTests : IClassFixture<AvailableServicesApiFactory>
{
    private readonly AvailableServicesApiFactory factory;

    public AvailableServicesHttpTests(AvailableServicesApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetAvailable_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/services/available");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailable_WithTelegramGuestToken_ReturnsPublicCatalog()
    {
        using var client = factory.CreateGuestClient();

        var response = await client.GetAsync("/api/services/available");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var service = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Consulta general", service.GetProperty("name").GetString());
        Assert.Equal(30, service.GetProperty("durationMinutes").GetInt32());
        Assert.Equal(55_000m, service.GetProperty("price").GetDecimal());
    }
}

public sealed class AvailableServicesApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.Services.Tests";
    private const string Audience = "Veterinaria.Client.Services.Tests";
    private const string KeyId = "services-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public AvailableServicesApiFactory()
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
    }

    public HttpClient CreateGuestClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateGuestToken());
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var sender = Substitute.For<ISender>();
            sender.Send(
                    Arg.Any<GetAvailableServicesQuery>(),
                    Arg.Any<CancellationToken>())
                .Returns(new[]
                {
                    new Service(Guid.NewGuid(), "Consulta general", 30, 55_000m)
                });
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

    private static string CreateGuestToken()
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
                new Claim("role", "TelegramGuest")
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
