using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Permissions.Claims;
using Domain.Appointments.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Reports;

// Contrato HTTP de GET /api/reports/appointments-by-day: usa el pipeline real de MediatR
// (autenticación, RequirePermission("Citas", View), FluentValidation) y solo sustituye el
// acceso a datos (IUnitOfWork), igual que otros HTTP tests de Reports/Citas.
public sealed class AppointmentsByDayReportHttpTests : IClassFixture<AppointmentsByDayReportApiFactory>
{
    private readonly AppointmentsByDayReportApiFactory factory;

    public AppointmentsByDayReportHttpTests(AppointmentsByDayReportApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetAppointmentsByDay_without_token_returns_401()
    {
        using var client = factory.CreateGuestClient();

        using var response = await client.GetAsync(
            "/api/reports/appointments-by-day?from=2026-09-01&to=2026-09-03");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointmentsByDay_without_Citas_View_returns_403()
    {
        using var client = factory.CreateAuthenticatedClient(withCitasView: false);

        using var response = await client.GetAsync(
            "/api/reports/appointments-by-day?from=2026-09-01&to=2026-09-03");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointmentsByDay_with_from_after_to_returns_400()
    {
        using var client = factory.CreateAuthenticatedClient(withCitasView: true);

        using var response = await client.GetAsync(
            "/api/reports/appointments-by-day?from=2026-09-03&to=2026-09-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointmentsByDay_returns_all_days_ordered_ascending_with_zero_counts()
    {
        using var client = factory.CreateAuthenticatedClient(withCitasView: true);

        using var response = await client.GetAsync(
            "/api/reports/appointments-by-day?from=2026-09-01&to=2026-09-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.EnumerateArray().ToArray();

        Assert.Equal(3, items.Length);
        Assert.Equal("2026-09-01", items[0].GetProperty("date").GetString());
        Assert.Equal("2026-09-02", items[1].GetProperty("date").GetString());
        Assert.Equal("2026-09-03", items[2].GetProperty("date").GetString());
        Assert.All(items, item =>
        {
            Assert.Equal(0, item.GetProperty("totalAppointments").GetInt32());
            Assert.Equal(0, item.GetProperty("attendedCount").GetInt32());
            Assert.Equal(0, item.GetProperty("canceledCount").GetInt32());
            Assert.Equal(0, item.GetProperty("scheduledCount").GetInt32());
        });
    }
}

public sealed class AppointmentsByDayReportApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.Reports.Tests";
    private const string Audience = "Veterinaria.Client.Reports.Tests";
    private const string KeyId = "reports-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public AppointmentsByDayReportApiFactory()
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

        var appointmentsRepository = Substitute.For<IAppointmentRepository>();
        appointmentsRepository.GetScheduledBetweenAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
    }

    public HttpClient CreateGuestClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public HttpClient CreateAuthenticatedClient(bool withCitasView)
    {
        var client = CreateGuestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(withCitasView));
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

    private static string CreateToken(bool withCitasView)
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

        if (withCitasView)
        {
            token.Payload[PermissionClaimValue.ClaimType] = new[]
            {
                PermissionClaimValue.Create("Citas", "View")
            };
        }

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
