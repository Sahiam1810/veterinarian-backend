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

// Etapa 3: Request (3.1) + Confirm (3.2) con fakes; sin Gmail/Oracle reales.
public sealed class ContactVerificationHttpTests : IClassFixture<ContactVerificationApiFactory>
{
    private readonly ContactVerificationApiFactory factory;

    public ContactVerificationHttpTests(ContactVerificationApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task RequestEmail_ValidRegister_Returns202_WithSafeMetadata_WithoutOtp()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "owner@huellitas.test", Purpose = "Register" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionId));
        Assert.NotEqual(Guid.Empty, sessionId.GetGuid());
        Assert.True(document.RootElement.TryGetProperty("expiresAt", out _));
        Assert.Equal("Email", document.RootElement.GetProperty("channel").GetString());
        Assert.False(document.RootElement.TryGetProperty("code", out _));
        Assert.False(document.RootElement.TryGetProperty("otp", out _));
        Assert.DoesNotContain("654321", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestEmail_InvalidEmail_Returns400_WithEmailInvalidCode()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "not-an-email", Purpose = "Register" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "ContactVerification.EmailInvalid",
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
    public async Task RequestEmail_ExceedsRateLimit_Returns429_WithProblemJson()
    {
        using var limitedFactory = new ContactVerificationRateLimitedApiFactory();
        using var client = limitedFactory.CreateAnonymousClient();

        using var first = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "rate@huellitas.test", Purpose = "Register" });
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);

        using var second = await client.PostAsJsonAsync(
            "/api/contact-verification/email/request",
            new { Email = "rate@huellitas.test", Purpose = "Register" });

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        using var document = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(429, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("RateLimit.Exceeded", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConfirmEmail_ExceedsRateLimit_Returns429_WithProblemJson()
    {
        using var limitedFactory = new ContactVerificationRateLimitedApiFactory();
        using var client = limitedFactory.CreateAnonymousClient();

        using var first = await client.PostAsJsonAsync(
            "/api/contact-verification/email/confirm",
            new { SessionId = Guid.NewGuid(), Code = "123456" });
        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);

        using var second = await client.PostAsJsonAsync(
            "/api/contact-verification/email/confirm",
            new { SessionId = Guid.NewGuid(), Code = "123456" });

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        using var document = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(429, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("RateLimit.Exceeded", document.RootElement.GetProperty("code").GetString());
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
            // Request real (3.1) mockeado: sin SMTP/Oracle; Confirm sigue usando repo nulo → 404.
            var request = Substitute.For<IRequestContactEmailVerification>();
            request.RequestAsync(Arg.Any<RequestContactEmailVerification>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var input = call.ArgAt<RequestContactEmailVerification>(0);
                    if (string.IsNullOrWhiteSpace(input.Email) || !input.Email.Contains('@'))
                    {
                        throw new Application.ContactVerification.Errors.ContactVerificationException(
                            Application.ContactVerification.Errors.ContactVerificationErrors.EmailInvalid);
                    }

                    return new RequestContactEmailVerificationResult(
                        Guid.NewGuid(),
                        DateTime.UtcNow.AddMinutes(10),
                        ContactVerificationChannel.Email);
                });

            var sessions = Substitute.For<IContactVerificationSessionRepository>();
            sessions.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((ContactVerificationSession?)null);

            services.RemoveAll<IRequestContactEmailVerification>();
            services.AddSingleton(request);
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
            ["RateLimiting__ContactEmailRequestWindowSeconds"] = "60",
            ["RateLimiting__ContactEmailConfirmPermitLimit"] = "1",
            ["RateLimiting__ContactEmailConfirmWindowSeconds"] = "60"
        };
}
