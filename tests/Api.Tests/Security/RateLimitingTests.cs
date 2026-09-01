using System.Net;
using System.Net.Http.Json;
using Api.Auth.Controllers;
using Application.Common.Results;
using Application.Security.Abstractions;
using Application.Security.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests.Security;

// Antes de este fix, Program.cs armaba el rate limiting con
// AddFixedWindowLimiter sin partición (un único contador global compartido
// por todos los clientes) y la implementación particionada/configurable de
// RateLimitingExtensions.AddApiRateLimiting nunca se conectaba. Estas
// pruebas confirman que ahora sí está conectada y produce la respuesta
// 429 con el formato problem+json esperado.
public sealed class RateLimitingTests : IClassFixture<RateLimitedApiFactory>
{
    private readonly RateLimitedApiFactory factory;

    public RateLimitingTests(RateLimitedApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Login_returns_429_with_problem_json_once_the_configured_limit_is_exceeded()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage? last = null;
        for (var i = 0; i < RateLimitedApiFactory.LoginPermitLimit; i++)
        {
            last?.Dispose();
            last = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { Email = "someone@huellitas.test", Password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, last.StatusCode);
        }

        last?.Dispose();

        using var rejected = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "someone@huellitas.test", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(
            "application/problem+json",
            rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }
}

public sealed class RateLimitedApiFactory : WebApplicationFactory<AuthController>
{
    public const int LoginPermitLimit = 2;

    private static readonly Api.Tests.Support.RsaTestKeys Keys = Api.Tests.Support.RsaTestKeys.Create();

    private static readonly IReadOnlyDictionary<string, string> TestEnvironment =
        new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.test",
            ["Jwt__Audience"] = "huellitas-api-ratelimit-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "ratelimit-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__LoginPermitLimit"] = $"{LoginPermitLimit}",
            ["RateLimiting__LoginWindowSeconds"] = "60",
            ["RateLimiting__RegisterPermitLimit"] = "1000",
            ["RateLimiting__RegisterWindowSeconds"] = "60",
            ["RateLimiting__RefreshPermitLimit"] = "1000",
            ["RateLimiting__RefreshWindowSeconds"] = "60",
            ["RateLimiting__TelegramWebhookPermitLimit"] = "1000",
            ["RateLimiting__TelegramWebhookWindowSeconds"] = "60"
        };

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public RateLimitedApiFactory()
    {
        foreach (var setting in TestEnvironment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService, AlwaysInvalidAuthenticationService>();
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

    private sealed class AlwaysInvalidAuthenticationService : IAuthenticationService
    {
        public Task<Result<CurrentProfile>> GetCurrentProfileAsync(
            Guid userAccountId,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected call.");

        public Task<Result<AuthenticationTokens>> RegisterAsync(
            string fullName,
            string email,
            string userName,
            string password,
            string identificationNumber,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected call.");

        public Task<Result<AuthenticationTokens>> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result<AuthenticationTokens>.Failure(
                Application.Security.Errors.AuthenticationErrors.InvalidCredentials));

        public Task<Result<AuthenticationTokens>> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected call.");

        public Task<Result> RevokeAsync(
            Guid userId,
            string refreshToken,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected call.");
    }
}
