using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.ContactVerification.Abstractions;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.ContactVerification;

// Etapa 3: request email sigue 501 (stub 3.1); confirm (3.2) usa handler real sin Oracle.
public sealed class ContactVerificationHttpTests : IClassFixture<ContactVerificationApiFactory>
{
    private readonly ContactVerificationApiFactory factory;

    public ContactVerificationHttpTests(ContactVerificationApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task RequestEmail_WithoutToken_Returns501_WithCatalogCode()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "owner@huellitas.test", Purpose = "Register" });

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "ContactVerification.NotImplemented",
            document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task RequestEmail_WithInvalidPurpose_Returns400()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "owner@huellitas.test", Purpose = "WhatsApp" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_UnknownSession_Returns404_WithSessionNotFoundCode()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/contact-verification/email/confirm",
            new { SessionId = Guid.NewGuid(), Code = "123456" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "ContactVerification.SessionNotFound",
            document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task RequestEmail_ExceedsRateLimit_Returns429()
    {
        using var limitedFactory = new ContactVerificationRateLimitedApiFactory();
        using var client = limitedFactory.CreateAnonymousClient();

        using var first = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "rate@huellitas.test", Purpose = "Register" });
        Assert.Equal(HttpStatusCode.NotImplemented, first.StatusCode);

        using var second = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "rate@huellitas.test", Purpose = "Register" });

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }
}

public class ContactVerificationApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ContactVerificationApiFactory()
        : this(CreateDefaultEnvironment())
    {
    }

    protected ContactVerificationApiFactory(Dictionary<string, string> environment)
    {
        foreach (var setting in environment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    private static Dictionary<string, string> CreateDefaultEnvironment() =>
        new()
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.contact-verification-tests",
            ["Jwt__Audience"] = "huellitas-api-contact-verification-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "contact-verification-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["Email__Enabled"] = "false",
            ["Twilio__Enabled"] = "false",
            ["Telegram__Enabled"] = "false",
            ["ContactVerification__OtpTtlMinutes"] = "10",
            ["ContactVerification__OtpMaximumAttempts"] = "5",
            ["ContactVerification__OtpResendSeconds"] = "60",
            ["ContactVerification__ProofTtlMinutes"] = "15"
        };

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
            var sessions = Substitute.For<IContactVerificationSessionRepository>();
            sessions.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((ContactVerificationSession?)null);
            services.RemoveAll<IContactVerificationSessionRepository>();
            services.AddSingleton(sessions);
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

// Factory con permit=1 para ejercitar HTTP 429 del endpoint request (sin Gmail).
public sealed class ContactVerificationRateLimitedApiFactory : ContactVerificationApiFactory
{
    private static readonly RsaTestKeys RateLimitKeys = RsaTestKeys.Create();

    public ContactVerificationRateLimitedApiFactory()
        : base(CreateRateLimitedEnvironment())
    {
    }

    private static Dictionary<string, string> CreateRateLimitedEnvironment() =>
        new()
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.contact-rate-limit-tests",
            ["Jwt__Audience"] = "huellitas-api-contact-rate-limit-tests",
            ["Jwt__PrivateKeyPemBase64"] = RateLimitKeys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = RateLimitKeys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "contact-rate-limit-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["Email__Enabled"] = "false",
            ["Twilio__Enabled"] = "false",
            ["Telegram__Enabled"] = "false",
            ["ContactVerification__OtpTtlMinutes"] = "10",
            ["ContactVerification__OtpMaximumAttempts"] = "5",
            ["ContactVerification__OtpResendSeconds"] = "60",
            ["ContactVerification__ProofTtlMinutes"] = "15",
            ["RateLimiting__ContactEmailRequestPermitLimit"] = "1",
            ["RateLimiting__ContactEmailRequestWindowSeconds"] = "60"
        };
}
