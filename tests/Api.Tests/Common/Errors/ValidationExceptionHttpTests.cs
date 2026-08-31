using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Common.Results;
using Application.Security.Abstractions;
using Application.Security.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests.Common.Errors;

public sealed class ValidationExceptionHttpTests : IClassFixture<OracleFreeApiFactory>
{
    private readonly HttpClient client;

    public ValidationExceptionHttpTests(OracleFreeApiFactory factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Post_invalid_login_returns_bad_request_with_email_validation_violation()
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "correo-invalido", Password = "secret" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;

        Assert.Equal(StatusCodes.Status400BadRequest, body.GetProperty("status").GetInt32());
        Assert.Equal("Bad Request", body.GetProperty("error").GetString());
        Assert.Equal("Validation failed", body.GetProperty("message").GetString());
        Assert.Equal("/api/auth/login", body.GetProperty("path").GetString());
        Assert.Contains(
            body.GetProperty("violations").EnumerateArray(),
            violation => violation.GetProperty("field").GetString() == "email");
    }
}

public sealed class OracleFreeApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private static readonly IReadOnlyDictionary<string, string> TestEnvironment =
        new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.test",
            ["Jwt__Audience"] = "huellitas-api-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "validation-errors-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0"
        };

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public OracleFreeApiFactory()
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
            services.AddSingleton<IAuthenticationService, OracleRejectingAuthenticationService>();
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

    private sealed class OracleRejectingAuthenticationService : IAuthenticationService
    {
        private static InvalidOperationException UnexpectedCall() =>
            new("Validation must stop the request before authentication or Oracle access.");

        public Task<Result<AuthenticationTokens>> RegisterAsync(
            string fullName,
            string email,
            string userName,
            string password,
            CancellationToken cancellationToken) =>
            throw UnexpectedCall();

        public Task<Result<AuthenticationTokens>> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            throw UnexpectedCall();

        public Task<Result<AuthenticationTokens>> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken) =>
            throw UnexpectedCall();

        public Task<Result> RevokeAsync(
            Guid userId,
            string refreshToken,
            CancellationToken cancellationToken) =>
            throw UnexpectedCall();

        public Task<Result<CurrentProfile>> GetCurrentProfileAsync(
            Guid userAccountId,
            CancellationToken cancellationToken) =>
            throw UnexpectedCall();
    }
}
