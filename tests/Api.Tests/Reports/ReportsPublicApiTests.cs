using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Permissions.Claims;
using Application.Reports.Abstraction;
using Application.Reports.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Reports;

public sealed class ReportsPublicApiTests : IClassFixture<ReportsPublicApiFactory>
{
    private readonly ReportsPublicApiFactory factory;

    public ReportsPublicApiTests(ReportsPublicApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetTopServices_authorized_returns_200_with_expected_shape()
    {
        var serviceId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        factory.Reports.GetTopServicesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(
                10,
                [new ServiceAppointmentCount(serviceId, "Consulta", 4)]));

        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(
            "/api/reports/top-services?from=2026-09-01&to=2026-09-07&take=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.EnumerateArray().ToArray();
        Assert.Single(items);
        Assert.Equal(serviceId, items[0].GetProperty("serviceId").GetGuid());
        Assert.Equal("Consulta", items[0].GetProperty("serviceName").GetString());
        Assert.Equal(4, items[0].GetProperty("appointmentsCount").GetInt32());
        Assert.Equal(40.0m, items[0].GetProperty("percentage").GetDecimal());
    }

    [Fact]
    public async Task GetSummary_authorized_returns_200_including_from_and_to()
    {
        factory.Reports.GetSummaryAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(0, 0, 0, 0, 0, null));

        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(
            "/api/reports/summary?from=2026-09-01&to=2026-09-07");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("2026-09-01", root.GetProperty("from").GetString());
        Assert.Equal("2026-09-07", root.GetProperty("to").GetString());
        Assert.Equal(0, root.GetProperty("totalAppointments").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("topService").ValueKind);
    }

    [Theory]
    [InlineData("/api/reports/top-services?from=2026-09-03&to=2026-09-01")]
    [InlineData("/api/reports/summary?from=2026-09-03&to=2026-09-01")]
    public async Task From_after_to_returns_400(string url)
    {
        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/reports/top-services?from=2025-01-01&to=2026-01-02")]
    [InlineData("/api/reports/summary?from=2025-01-01&to=2026-01-02")]
    public async Task Inclusive_367_days_returns_400(string url)
    {
        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/reports/top-services?from=2025-01-01&to=2026-01-01")]
    [InlineData("/api/reports/summary?from=2025-01-01&to=2026-01-01")]
    public async Task Inclusive_366_days_is_valid(string url)
    {
        factory.Reports.GetTopServicesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(0, Array.Empty<ServiceAppointmentCount>()));
        factory.Reports.GetSummaryAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(0, 0, 0, 0, 0, null));

        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Take_0_returns_400()
    {
        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(
            "/api/reports/top-services?from=2026-09-01&to=2026-09-01&take=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Take_21_returns_400()
    {
        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(
            "/api/reports/top-services?from=2026-09-01&to=2026-09-01&take=21");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/reports/top-services?from=2026-09-01&to=2026-09-01")]
    [InlineData("/api/reports/summary?from=2026-09-01&to=2026-09-01")]
    public async Task Unauthenticated_returns_401(string url)
    {
        using var client = factory.CreateGuestClient();

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/reports/top-services?from=2026-09-01&to=2026-09-01")]
    [InlineData("/api/reports/summary?from=2026-09-01&to=2026-09-01")]
    public async Task Authenticated_without_Reportes_View_returns_403(string url)
    {
        using var client = factory.CreateAuthenticatedClient(withReportesView: false);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/reports/top-services?from=2026-09-01&to=2026-09-01")]
    [InlineData("/api/reports/summary?from=2026-09-01&to=2026-09-01")]
    public async Task Authorized_with_Reportes_View_returns_200(string url)
    {
        factory.Reports.GetTopServicesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(0, Array.Empty<ServiceAppointmentCount>()));
        factory.Reports.GetSummaryAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(0, 0, 0, 0, 0, null));

        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Date_conversion_uses_inclusive_start_and_exclusive_end_in_bogota()
    {
        factory.Reports.ClearReceivedCalls();
        factory.Reports.GetTopServicesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(0, Array.Empty<ServiceAppointmentCount>()));

        using var client = factory.CreateAuthenticatedClient(withReportesView: true);

        using var response = await client.GetAsync(
            "/api/reports/top-services?from=2026-09-01&to=2026-09-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await factory.Reports.Received().GetTopServicesAsync(
            new DateTime(2026, 9, 1, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 4, 5, 0, 0, DateTimeKind.Utc),
            5,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DateOnly_parameters_are_documented_as_date_query_params()
    {
        var descriptions = factory.Services
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items);

        var topServices = Assert.Single(
            descriptions,
            description => description.HttpMethod == HttpMethod.Get.Method &&
                           description.RelativePath == "api/Reports/top-services");
        var summary = Assert.Single(
            descriptions,
            description => description.HttpMethod == HttpMethod.Get.Method &&
                           description.RelativePath == "api/Reports/summary");

        AssertDateOnlyQuery(topServices, "from");
        AssertDateOnlyQuery(topServices, "to");
        var take = Assert.Single(topServices.ParameterDescriptions, parameter => parameter.Name == "take");
        Assert.False(take.IsRequired);

        AssertDateOnlyQuery(summary, "from");
        AssertDateOnlyQuery(summary, "to");

        Assert.Contains(topServices.SupportedResponseTypes, response => response.StatusCode == 200);
        Assert.Contains(topServices.SupportedResponseTypes, response => response.StatusCode == 400);
        Assert.Contains(topServices.SupportedResponseTypes, response => response.StatusCode == 401);
        Assert.Contains(topServices.SupportedResponseTypes, response => response.StatusCode == 403);
        Assert.Contains(summary.SupportedResponseTypes, response => response.StatusCode == 200);
        Assert.Contains(summary.SupportedResponseTypes, response => response.StatusCode == 400);
        Assert.Contains(summary.SupportedResponseTypes, response => response.StatusCode == 401);
        Assert.Contains(summary.SupportedResponseTypes, response => response.StatusCode == 403);
    }

    private static void AssertDateOnlyQuery(ApiDescription endpoint, string name)
    {
        var parameter = Assert.Single(
            endpoint.ParameterDescriptions,
            parameter => parameter.Name == name);
        Assert.Equal(typeof(DateOnly), parameter.Type);
        Assert.Equal("query", parameter.Source.Id, ignoreCase: true);
    }
}

public sealed class ReportsPublicApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.Reports.Public.Tests";
    private const string Audience = "Veterinaria.Client.Reports.Public.Tests";
    private const string KeyId = "reports-public-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ReportsPublicApiFactory()
    {
        Reports = Substitute.For<IReportsReadRepository>();
        Reports.GetTopServicesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(0, Array.Empty<ServiceAppointmentCount>()));
        Reports.GetSummaryAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(0, 0, 0, 0, 0, null));

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

    public IReportsReadRepository Reports { get; }

    public HttpClient CreateGuestClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public HttpClient CreateAuthenticatedClient(bool withReportesView)
    {
        var client = CreateGuestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(withReportesView));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IReportsReadRepository>();
            services.AddSingleton(Reports);
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

    private static string CreateToken(bool withReportesView)
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

        if (withReportesView)
        {
            token.Payload[PermissionClaimValue.ClaimType] = new[]
            {
                PermissionClaimValue.Create("Reportes", "View")
            };
        }

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
