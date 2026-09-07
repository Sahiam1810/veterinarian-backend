using System.Net;
using System.Text;
using Api.Auth.Controllers;
using Api.Telegram.Security;
using Api.Tests.Support;
using Application.Telegram.Updates;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Telegram;

// Tarea 6.1: POST webhook Telegram anónimo + rate limit HTTP 429 (sin Telegram real).
[Collection(EnvironmentVariablesCollection.Name)]
public sealed class TelegramWebhookRateLimitHttpTests
{
    private const string WebhookPath = "/api/integrations/telegram/webhook";

    [Fact]
    public async Task Receive_Returns429_WithRateLimitExceeded_WhenPermitLimitIsExceeded()
    {
        using var factory = new TelegramWebhookRateLimitedApiFactory();
        using var client = factory.CreateAnonymousClient();

        using var first = await client.PostAsync(WebhookPath, CreateUpdateContent());
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        factory.Sender.ClearReceivedCalls();

        using var second = await client.PostAsync(WebhookPath, CreateUpdateContent());
        await RateLimitExceededAssert.EqualsContractAsync(second);

        await factory.Sender.DidNotReceive()
            .Send(Arg.Any<IngestTelegramUpdateCommand>(), Arg.Any<CancellationToken>());
    }

    private static StringContent CreateUpdateContent() =>
        new(
            """{"update_id":1,"message":{"message_id":1,"from":{"id":1001},"chat":{"id":1001,"type":"private"},"text":"ping"}}""",
            Encoding.UTF8,
            "application/json");
}

public sealed class TelegramWebhookRateLimitedApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();

    public TelegramWebhookRateLimitedApiFactory()
    {
        foreach (var setting in BuildEnvironment())
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        Sender.Send(Arg.Any<IngestTelegramUpdateCommand>(), Arg.Any<CancellationToken>())
            .Returns(IngestTelegramUpdateResult.Accepted);
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
            var secretValidator = Substitute.For<ITelegramWebhookSecretValidator>();
            secretValidator.IsValid(Arg.Any<string?>()).Returns(true);

            services.RemoveAll<ISender>();
            services.AddSingleton(Sender);
            services.RemoveAll<ITelegramWebhookSecretValidator>();
            services.AddSingleton(secretValidator);
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
            ["Jwt__Issuer"] = "https://issuer.huellitas.telegram-webhook-rate-limit-tests",
            ["Jwt__Audience"] = "huellitas-api-telegram-webhook-rate-limit-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "telegram-webhook-rate-limit-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__TelegramWebhookPermitLimit"] = "1",
            ["RateLimiting__TelegramWebhookWindowSeconds"] = "60"
        };
}
