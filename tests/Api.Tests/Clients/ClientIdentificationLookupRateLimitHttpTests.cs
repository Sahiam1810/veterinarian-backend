using System.Net;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Clients.UseCases;
using Domain.Clients.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Clients;

// Tarea 6.1: GET /api/clients/by-identification/{id} anónimo + rate limit HTTP 429.
[Collection(EnvironmentVariablesCollection.Name)]
public sealed class ClientIdentificationLookupRateLimitHttpTests
{
    // Fixture sintético de test (solo dígitos; no cédula real).
    private const string FixtureIdentification = "9001002003";

    [Fact]
    public async Task GetByIdentification_Returns429_WithRateLimitExceeded_WhenPermitLimitIsExceeded()
    {
        using var factory = new ClientIdentificationLookupRateLimitedApiFactory();
        using var client = factory.CreateAnonymousClient();

        using var first = await client.GetAsync($"/api/clients/by-identification/{FixtureIdentification}");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        factory.Sender.ClearReceivedCalls();

        using var second = await client.GetAsync($"/api/clients/by-identification/{FixtureIdentification}");
        await RateLimitExceededAssert.EqualsContractAsync(second);

        await factory.Sender.DidNotReceive()
            .Send(Arg.Any<GetClientByIdentificationQuery>(), Arg.Any<CancellationToken>());
    }
}

public sealed class ClientIdentificationLookupRateLimitedApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();

    public ClientIdentificationLookupRateLimitedApiFactory()
    {
        foreach (var setting in BuildEnvironment())
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        var knownClient = new ClientEntity(
            Guid.Parse("cccccccc-3333-3333-3333-333333333333"),
            "9001002003",
            "N/A",
            registrationDate: new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            phoneNumber: "3000000001");

        Sender.Send(Arg.Any<GetClientByIdentificationQuery>(), Arg.Any<CancellationToken>())
            .Returns(knownClient);
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
            ["Jwt__Issuer"] = "https://issuer.huellitas.id-lookup-rate-limit-tests",
            ["Jwt__Audience"] = "huellitas-api-id-lookup-rate-limit-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "id-lookup-rate-limit-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__ClientIdentificationLookupPermitLimit"] = "1",
            ["RateLimiting__ClientIdentificationLookupWindowSeconds"] = "60"
        };
}
