using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Auth.Dtos;
using Api.Common.Security;
using Api.Tests.Support;
using Application.Common.Results;
using Application.Security.EmailOtp;
using Application.Security.Errors;
using Application.Security.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests.Security;

// Tarea 3.3: Endpoints anónimos para Email OTP con Rate Limiting y RFC 7807 problem+json.
public sealed class EmailOtpHttpTests : IClassFixture<EmailOtpApiFactory>
{
    private readonly EmailOtpApiFactory factory;

    public EmailOtpHttpTests(EmailOtpApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task RequestOtp_WithoutToken_Returns200_WithGenericMessage_AndNeverLeaksOtp()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/email-otp/request",
            new RequestEmailOtpRequest("client@huellitas.test"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        // Debe contener solo mensaje genérico informativo
        Assert.True(
            root.TryGetProperty("message", out var messageProp) ||
            root.TryGetProperty("Message", out messageProp));
        Assert.False(string.IsNullOrWhiteSpace(messageProp.GetString()));

        // CRÍTICO: Ninguna propiedad debe exponer código OTP ni secretos
        Assert.False(root.TryGetProperty("otp", out _));
        Assert.False(root.TryGetProperty("Otp", out _));
        Assert.False(root.TryGetProperty("code", out _));
        Assert.False(root.TryGetProperty("Code", out _));
        Assert.False(root.TryGetProperty("token", out _));
        Assert.False(root.TryGetProperty("secret", out _));
        Assert.False(root.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task RequestOtp_WithInvalidEmail_Returns400_BadRequest()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/email-otp/request",
            new RequestEmailOtpRequest("not-a-valid-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmOtp_WithoutToken_WithValidCode_Returns200_WithTokens()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/email-otp/confirm",
            new ConfirmEmailOtpRequest(EmailOtpApiFactory.ValidEmail, EmailOtpApiFactory.ValidCode));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.True(
            root.TryGetProperty("accessToken", out var accessToken) ||
            root.TryGetProperty("AccessToken", out accessToken));
        Assert.False(string.IsNullOrWhiteSpace(accessToken.GetString()));

        Assert.True(
            root.TryGetProperty("refreshToken", out var refreshToken) ||
            root.TryGetProperty("RefreshToken", out refreshToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken.GetString()));

        Assert.True(
            root.TryGetProperty("accessTokenExpiresAt", out _) ||
            root.TryGetProperty("AccessTokenExpiresAt", out _));
        Assert.True(
            root.TryGetProperty("refreshTokenExpiresAt", out _) ||
            root.TryGetProperty("RefreshTokenExpiresAt", out _));
    }

    [Fact]
    public async Task ConfirmOtp_WithInvalidCode_Returns401_WithProblemJson()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/email-otp/confirm",
            new ConfirmEmailOtpRequest(EmailOtpApiFactory.ValidEmail, "000000"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(401, root.GetProperty("status").GetInt32());
        Assert.Equal(AuthenticationErrors.InvalidOtp.Code, root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConfirmOtp_WithInvalidEmail_Returns400_BadRequest()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/email-otp/confirm",
            new ConfirmEmailOtpRequest("invalid-email", "123456"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class EmailOtpRateLimitingHttpTests : IClassFixture<EmailOtpRateLimitedApiFactory>
{
    private readonly EmailOtpRateLimitedApiFactory factory;

    public EmailOtpRateLimitingHttpTests(EmailOtpRateLimitedApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task RequestOtp_Returns429_WhenRateLimitIsExceeded()
    {
        using var client = factory.CreateAnonymousClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < EmailOtpRateLimitedApiFactory.EmailOtpPermitLimit; i++)
        {
            last?.Dispose();
            last = await client.PostAsJsonAsync(
                "/api/auth/email-otp/request",
                new RequestEmailOtpRequest("ratelimit@huellitas.test"));
            Assert.Equal(HttpStatusCode.OK, last.StatusCode);
        }

        last?.Dispose();

        using var rejected = await client.PostAsJsonAsync(
            "/api/auth/email-otp/request",
            new RequestEmailOtpRequest("ratelimit@huellitas.test"));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(
            "application/problem+json",
            rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.Contains("Retry-After"));

        using var document = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(429, root.GetProperty("status").GetInt32());
        Assert.Equal("RateLimit.Exceeded", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConfirmOtp_Returns429_WhenRateLimitIsExceeded()
    {
        using var client = factory.CreateAnonymousClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < EmailOtpRateLimitedApiFactory.EmailOtpPermitLimit; i++)
        {
            last?.Dispose();
            last = await client.PostAsJsonAsync(
                "/api/auth/email-otp/confirm",
                new ConfirmEmailOtpRequest(EmailOtpApiFactory.ValidEmail, EmailOtpApiFactory.ValidCode));
            Assert.Equal(HttpStatusCode.OK, last.StatusCode);
        }

        last?.Dispose();

        using var rejected = await client.PostAsJsonAsync(
            "/api/auth/email-otp/confirm",
            new ConfirmEmailOtpRequest(EmailOtpApiFactory.ValidEmail, EmailOtpApiFactory.ValidCode));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(
            "application/problem+json",
            rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.Contains("Retry-After"));

        using var document = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(429, root.GetProperty("status").GetInt32());
        Assert.Equal("RateLimit.Exceeded", root.GetProperty("code").GetString());
    }
}

public sealed class EmailOtpApiFactory : WebApplicationFactory<AuthController>
{
    public const string ValidEmail = "client@huellitas.test";
    public const string ValidCode = "123456";

    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public EmailOtpApiFactory()
    {
        foreach (var setting in BuildEnvironment())
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
            services.RemoveAll<IEmailOtpAuthenticationService>();
            services.AddSingleton<IEmailOtpAuthenticationService, FakeEmailOtpAuthenticationService>();
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

    internal static Dictionary<string, string> BuildEnvironment(
        int requestLimit = 1000,
        int confirmLimit = 1000) =>
        new()
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.test",
            ["Jwt__Audience"] = "huellitas-api-email-otp-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "email-otp-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__LoginPermitLimit"] = "1000",
            ["RateLimiting__LoginWindowSeconds"] = "60",
            ["RateLimiting__RefreshPermitLimit"] = "1000",
            ["RateLimiting__RefreshWindowSeconds"] = "60",
            ["RateLimiting__RegisterPermitLimit"] = "1000",
            ["RateLimiting__RegisterWindowSeconds"] = "60",
            ["RateLimiting__TelegramWebhookPermitLimit"] = "1000",
            ["RateLimiting__TelegramWebhookWindowSeconds"] = "60",
            ["RateLimiting__EmailOtpRequestPermitLimit"] = $"{requestLimit}",
            ["RateLimiting__EmailOtpRequestWindowSeconds"] = "60",
            ["RateLimiting__EmailOtpConfirmPermitLimit"] = $"{confirmLimit}",
            ["RateLimiting__EmailOtpConfirmWindowSeconds"] = "60"
        };
}

public sealed class EmailOtpRateLimitedApiFactory : WebApplicationFactory<AuthController>
{
    public const int EmailOtpPermitLimit = 2;

    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public EmailOtpRateLimitedApiFactory()
    {
        foreach (var setting in EmailOtpApiFactory.BuildEnvironment(EmailOtpPermitLimit, EmailOtpPermitLimit))
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
            services.RemoveAll<IEmailOtpAuthenticationService>();
            services.AddSingleton<IEmailOtpAuthenticationService, FakeEmailOtpAuthenticationService>();
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

internal sealed class FakeEmailOtpAuthenticationService : IEmailOtpAuthenticationService
{
    public Task<Result> RequestOtpAsync(
        string email,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());

    public Task<Result<AuthenticationTokens>> ConfirmOtpAsync(
        string email,
        string code,
        CancellationToken cancellationToken)
    {
        if (email == EmailOtpApiFactory.ValidEmail && code == EmailOtpApiFactory.ValidCode)
        {
            var tokens = new AuthenticationTokens(
                "fake-access-token",
                DateTimeOffset.UtcNow.AddMinutes(15),
                "fake-refresh-token",
                DateTimeOffset.UtcNow.AddDays(7));

            return Task.FromResult(Result<AuthenticationTokens>.Success(tokens));
        }

        return Task.FromResult(Result<AuthenticationTokens>.Failure(
            AuthenticationErrors.InvalidOtp));
    }
}
