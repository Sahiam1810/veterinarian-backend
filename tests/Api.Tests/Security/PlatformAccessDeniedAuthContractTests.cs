using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Auth.Controllers;
using Application.Common.Results;
using Application.Security.Abstractions;
using Application.Security.Errors;
using Application.Security.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests.Security;

public sealed class PlatformAccessDeniedAuthContractTests
    : IClassFixture<PlatformAccessDeniedAuthContractApiFactory>
{
    private readonly PlatformAccessDeniedAuthContractApiFactory factory;

    public PlatformAccessDeniedAuthContractTests(
        PlatformAccessDeniedAuthContractApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task Login_PlatformAccessDenied_returns_403_problem_json_with_stable_code()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "cliente@huellitas.test", Password = "anything" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(
            AuthenticationErrors.PlatformAccessDenied.Code,
            root.GetProperty("code").GetString());
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", root.GetProperty("title").GetString());

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("cliente@", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cliente", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PASSWORD", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detail", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_PlatformAccessDenied_returns_403_problem_json_with_stable_code()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { RefreshToken = "legacy-client-refresh" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(
            AuthenticationErrors.PlatformAccessDenied.Code,
            root.GetProperty("code").GetString());
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.False(root.TryGetProperty("accessToken", out _));
        Assert.False(root.TryGetProperty("refreshToken", out _));

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Cliente", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detail", payload, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PlatformAccessDeniedAuthContractApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly Api.Tests.Support.RsaTestKeys Keys =
        Api.Tests.Support.RsaTestKeys.Create();

    private static readonly IReadOnlyDictionary<string, string> TestEnvironment =
        new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.test",
            ["Jwt__Audience"] = "huellitas-api-platform-access-denied-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "platform-access-denied-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__LoginPermitLimit"] = "1000",
            ["RateLimiting__LoginWindowSeconds"] = "60",
            ["RateLimiting__RegisterPermitLimit"] = "1000",
            ["RateLimiting__RegisterWindowSeconds"] = "60",
            ["RateLimiting__RefreshPermitLimit"] = "1000",
            ["RateLimiting__RefreshWindowSeconds"] = "60",
            ["RateLimiting__TelegramWebhookPermitLimit"] = "1000",
            ["RateLimiting__TelegramWebhookWindowSeconds"] = "60"
        };

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public PlatformAccessDeniedAuthContractApiFactory()
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
            services.AddSingleton<IAuthenticationService, PlatformAccessDeniedAuthenticationService>();
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

    private sealed class PlatformAccessDeniedAuthenticationService : IAuthenticationService
    {
        public Task<Result<CurrentProfile>> GetCurrentProfileAsync(
            Guid userAccountId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Unexpected call.");

        public Task<Result<AuthenticationTokens>> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.PlatformAccessDenied));

        public Task<Result<AuthenticationTokens>> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.PlatformAccessDenied));

        public Task<Result> RevokeAsync(
            Guid userId,
            string refreshToken,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Unexpected call.");
    }
}
