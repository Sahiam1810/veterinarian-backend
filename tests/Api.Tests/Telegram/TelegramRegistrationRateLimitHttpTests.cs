using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Common.Results;
using Application.Telegram.Registration;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Telegram;

// Tarea 6.1: GET/POST telegram/registration/complete + rate limit 429 (sin Telegram real).
[Collection(EnvironmentVariablesCollection.Name)]
public sealed class TelegramRegistrationRateLimitHttpTests
{
    private const string Path = "/telegram/registration/complete";

    [Fact]
    public async Task CompleteGet_Returns429_WithRateLimitExceeded_WhenPermitLimitIsExceeded()
    {
        using var factory = new TelegramRegistrationRateLimitedApiFactory();
        using var client = factory.CreateAnonymousClient();

        using var first = await client.GetAsync(Path);
        Assert.NotEqual(System.Net.HttpStatusCode.TooManyRequests, first.StatusCode);

        using var second = await client.GetAsync(Path);
        await RateLimitExceededAssert.EqualsContractAsync(second);
    }
}

public sealed class TelegramRegistrationRateLimitedApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();

    public TelegramRegistrationRateLimitedApiFactory()
    {
        foreach (var setting in BuildEnvironment())
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        Sender.Send(Arg.Any<GetTelegramRegistrationSessionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PendingTelegramRegistration>.Failure(
                TelegramRegistrationErrors.InvalidOrExpired));
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
            services.RemoveAll<ISender>();
            services.AddSingleton(Sender);
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

    private static Dictionary<string, string> BuildEnvironment() =>
        new()
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Email__Enabled"] = "false",
            ["Twilio__Enabled"] = "false",
            ["Telegram__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.telegram-registration-rate-limit-tests",
            ["Jwt__Audience"] = "huellitas-api-telegram-registration-rate-limit-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "telegram-registration-rate-limit-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__TelegramRegistrationPermitLimit"] = "1",
            ["RateLimiting__TelegramRegistrationWindowSeconds"] = "60"
        };
}
