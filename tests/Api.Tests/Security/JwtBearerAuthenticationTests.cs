using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Common.Results;
using Application.Security.Abstractions;
using Application.Security.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Api.Tests.Security;

public sealed class JwtBearerAuthenticationTests : IClassFixture<JwtBearerApiFactory>
{
    private readonly JwtBearerApiFactory factory;

    public JwtBearerAuthenticationTests(JwtBearerApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task Me_accepts_rs256_token_signed_by_the_configured_private_key()
    {
        using var client = CreateClient(factory.CreateRs256Token());

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "cliente@huellitas.test",
            document.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Notifications_hub_negotiate_accepts_token_via_query_string()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = factory.CreateRs256Token();
        using var response = await client.PostAsync(
            $"/hubs/notifications/negotiate?negotiateVersion=1&access_token={token}",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_ignores_token_via_query_string_outside_the_notifications_hub()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = factory.CreateRs256Token();
        using var response = await client.GetAsync($"/api/auth/me?access_token={token}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidToken.WrongSigningKey)]
    [InlineData(InvalidToken.Hs256)]
    [InlineData(InvalidToken.WrongIssuer)]
    [InlineData(InvalidToken.WrongAudience)]
    [InlineData(InvalidToken.Expired)]
    [InlineData(InvalidToken.Malformed)]
    public async Task Me_rejects_invalid_token(InvalidToken invalidToken)
    {
        var token = invalidToken == InvalidToken.Malformed
            ? "not-a-jwt"
            : factory.CreateInvalidToken(invalidToken);
        using var client = CreateClient(token);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            Application.Security.Errors.AuthenticationErrors.Unauthorized.Code,
            document.RootElement.GetProperty("code").GetString());
    }

    private HttpClient CreateClient(string token)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public enum InvalidToken
{
    WrongSigningKey,
    Hs256,
    WrongIssuer,
    WrongAudience,
    Expired,
    Malformed
}

public sealed class JwtBearerApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.Jwt.Tests";
    private const string Audience = "Veterinaria.Client.Jwt.Tests";
    private const string KeyId = "jwt-http-test-key";
    private static readonly Guid AccountId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private static readonly IReadOnlyDictionary<string, string> TestEnvironment =
        new Dictionary<string, string>
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

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public JwtBearerApiFactory()
    {
        foreach (var setting in TestEnvironment)
        {
            originalEnvironment[setting.Key] =
                Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    public string CreateRs256Token() => CreateRs256Token(
        Keys,
        Issuer,
        Audience,
        DateTime.UtcNow.AddMinutes(5));

    public string CreateInvalidToken(InvalidToken invalidToken) => invalidToken switch
    {
        InvalidToken.WrongSigningKey => CreateRs256Token(
            RsaTestKeys.Create(), Issuer, Audience, DateTime.UtcNow.AddMinutes(5)),
        InvalidToken.Hs256 => CreateHs256Token(),
        InvalidToken.WrongIssuer => CreateRs256Token(
            Keys, "Another.Issuer", Audience, DateTime.UtcNow.AddMinutes(5)),
        InvalidToken.WrongAudience => CreateRs256Token(
            Keys, Issuer, "Another.Audience", DateTime.UtcNow.AddMinutes(5)),
        InvalidToken.Expired => CreateRs256Token(
            Keys, Issuer, Audience, DateTime.UtcNow.AddMinutes(-1)),
        InvalidToken.Malformed => throw new ArgumentOutOfRangeException(nameof(invalidToken)),
        _ => throw new ArgumentOutOfRangeException(nameof(invalidToken))
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService, ProfileAuthenticationService>();
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

    private static string CreateRs256Token(
        RsaTestKeys keys,
        string issuer,
        string audience,
        DateTime expires)
    {
        using var rsa = RSA.Create();
        var privatePem = Encoding.UTF8.GetString(
            Convert.FromBase64String(keys.PrivateKeyPemBase64));
        rsa.ImportFromPem(privatePem);

        var signingKey = new RsaSecurityKey(rsa)
        {
            KeyId = KeyId,
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = false
            }
        };

        return WriteToken(
            issuer,
            audience,
            expires,
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.RsaSha256));
    }

    private static string CreateHs256Token() => WriteToken(
        Issuer,
        Audience,
        DateTime.UtcNow.AddMinutes(5),
        new SigningCredentials(
            new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)),
            SecurityAlgorithms.HmacSha256));

    private static string WriteToken(
        string issuer,
        string audience,
        DateTime expires,
        SigningCredentials signingCredentials)
    {
        var notBefore = expires > DateTime.UtcNow
            ? DateTime.UtcNow.AddMinutes(-1)
            : expires.AddMinutes(-5);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [new Claim(JwtRegisteredClaimNames.Sub, AccountId.ToString())],
            notBefore,
            expires,
            signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class ProfileAuthenticationService : IAuthenticationService
    {
        public Task<Result<CurrentProfile>> GetCurrentProfileAsync(
            Guid userAccountId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result<CurrentProfile>.Success(new CurrentProfile(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                userAccountId,
                "Cliente Prueba",
                "CP",
                "cliente.prueba",
                "cliente@huellitas.test",
                "Cliente",
                "Activo")));

        public Task<Result<AuthenticationTokens>> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken) => throw UnexpectedCall();

        public Task<Result<AuthenticationTokens>> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken) => throw UnexpectedCall();

        public Task<Result> RevokeAsync(
            Guid userId,
            string refreshToken,
            CancellationToken cancellationToken) => throw UnexpectedCall();

        private static InvalidOperationException UnexpectedCall() =>
            new("The authentication test invoked an unrelated operation.");
    }
}
