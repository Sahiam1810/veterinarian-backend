// This test is the API route contract consumed by the frontend.
// If an intentional route change is made, update this contract and
// ordenesMedicasService.ts in the same change.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Auth.Controllers;
using Api.Common.Security.Permissions;
using Api.Tests.Support;
using Application.Permissions.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.MedicationOrders;

public sealed class MedicalOrdersRoutesTests : IClassFixture<MedicalOrdersRoutesApiFactory>
{
    private static readonly (string Method, string Pattern)[] ContractedRoutes =
    [
        ("POST", "api/medication-orders"),
        ("GET", "api/medication-orders/{id:guid}"),
        ("GET", "api/medication-orders/appointment/{appointmentId:guid}"),
        ("GET", "api/medication-orders/pending"),
        ("PATCH", "api/medication-orders/{id:guid}/complete"),
        ("POST", "api/procedure-orders"),
        ("GET", "api/procedure-orders/{id:guid}"),
        ("GET", "api/procedure-orders/appointment/{appointmentId:guid}"),
        ("GET", "api/procedure-orders/pending"),
        ("PATCH", "api/procedure-orders/{id:guid}/complete"),
        ("GET", "api/medications"),
        ("POST", "api/medications"),
        ("PUT", "api/medications/{id:guid}"),
        ("DELETE", "api/medications/{id:guid}"),
        ("GET", "api/procedures"),
        ("POST", "api/procedures"),
        ("PUT", "api/procedures/{id:guid}"),
        ("DELETE", "api/procedures/{id:guid}"),
    ];

    private readonly MedicalOrdersRoutesApiFactory factory;

    public MedicalOrdersRoutesTests(MedicalOrdersRoutesApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public void Contracted_medication_and_procedure_routes_are_registered()
    {
        using var _ = factory.CreateGuestClient();
        var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();

        var registered = new HashSet<(string Method, string Pattern)>(RoutePairComparer.Instance);
        foreach (var endpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
        {
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
            if (methods is null)
            {
                continue;
            }

            var pattern = NormalizePattern(endpoint.RoutePattern.RawText);
            foreach (var method in methods)
            {
                registered.Add((method, pattern));
            }
        }

        foreach (var (method, pattern) in ContractedRoutes)
        {
            Assert.True(
                registered.Contains((method, pattern)),
                $"Missing route {method} {pattern}");
        }
    }

    [Theory]
    [InlineData("/api/MedicationOrders")]
    [InlineData("/api/ProcedureOrders")]
    public async Task Legacy_pascal_case_order_paths_return_404(string path)
    {
        using var client = factory.CreateGuestClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/medication-orders/pending")]
    [InlineData("/api/procedure-orders/pending")]
    public async Task Pending_without_token_returns_401(string path)
    {
        using var client = factory.CreateGuestClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/medication-orders/pending")]
    [InlineData("/api/procedure-orders/pending")]
    public async Task Pending_authenticated_without_OrdenesMedicas_View_returns_403(string path)
    {
        using var client = factory.CreateAuthenticatedClient(withOrdenesMedicasView: false);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string NormalizePattern(string? rawText) =>
        (rawText ?? string.Empty).TrimStart('/');

    private sealed class RoutePairComparer : IEqualityComparer<(string Method, string Pattern)>
    {
        public static readonly RoutePairComparer Instance = new();

        public bool Equals((string Method, string Pattern) x, (string Method, string Pattern) y) =>
            string.Equals(x.Method, y.Method, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Pattern, y.Pattern, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Method, string Pattern) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Method),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Pattern));
    }
}

public sealed class MedicalOrdersRoutesApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.MedicalOrders.Routes.Tests";
    private const string Audience = "Veterinaria.Client.MedicalOrders.Routes.Tests";
    private const string KeyId = "medical-orders-routes-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];
    private readonly ISender sender = Substitute.For<ISender>();

    public MedicalOrdersRoutesApiFactory()
    {
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
    }

    public HttpClient CreateGuestClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public HttpClient CreateAuthenticatedClient(bool withOrdenesMedicasView)
    {
        var client = CreateGuestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(withOrdenesMedicasView));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISender>();
            services.AddSingleton(sender);

            // Production FallbackPolicy (RequireAuthenticatedUser) turns anonymous
            // unmatched routes into 401. Clear it here so missing kebab-case
            // aliases surface as true routing 404 — and so a regression to
            // api/[controller] would match and return 401 instead of 404.
            services.PostConfigure<AuthorizationOptions>(options =>
                options.FallbackPolicy = null);
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

    private static string CreateToken(bool withOrdenesMedicasView)
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
                new Claim("role_id", Guid.NewGuid().ToString())
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

        if (withOrdenesMedicasView)
        {
            token.Payload[PermissionClaimValue.ClaimType] = new[]
            {
                PermissionClaimValue.Create("Órdenes Médicas", PermissionAction.View.ToString())
            };
        }

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
