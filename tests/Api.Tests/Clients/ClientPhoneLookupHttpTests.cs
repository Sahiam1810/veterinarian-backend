using System.Net;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Clients.UseCases;
using Application.Common.Exceptions;
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

// Tarea 2.2: GET /api/clients/by-phone/{phone} anónimo + rate limit.
public sealed class ClientPhoneLookupHttpTests : IClassFixture<ClientPhoneLookupApiFactory>
{
    private readonly ClientPhoneLookupApiFactory factory;

    public ClientPhoneLookupHttpTests(ClientPhoneLookupApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task GetByPhone_WithoutToken_Returns200_WithMinimalDto()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync("/api/clients/by-phone/3001234567");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(factory.KnownClient.Id, root.GetProperty("id").GetGuid());
        Assert.Equal(factory.KnownClient.UserId, root.GetProperty("userId").GetGuid());
        Assert.Equal("1234567890", root.GetProperty("identificationNumber").GetString());
        Assert.False(root.TryGetProperty("address", out _));
        Assert.False(root.TryGetProperty("phoneNumber", out _));
    }

    [Fact]
    public async Task GetByPhone_WithoutToken_WhenMissing_Returns404()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync("/api/clients/by-phone/3009999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class ClientPhoneLookupRateLimitHttpTests
    : IClassFixture<ClientPhoneLookupRateLimitedApiFactory>
{
    private readonly ClientPhoneLookupRateLimitedApiFactory factory;

    public ClientPhoneLookupRateLimitHttpTests(ClientPhoneLookupRateLimitedApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task GetByPhone_Returns429_WhenPermitLimitIsExceeded()
    {
        using var client = factory.CreateAnonymousClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < ClientPhoneLookupRateLimitedApiFactory.PhoneLookupPermitLimit; i++)
        {
            last?.Dispose();
            last = await client.GetAsync("/api/clients/by-phone/3001234567");
            Assert.Equal(HttpStatusCode.OK, last.StatusCode);
        }

        last?.Dispose();
        using var rejected = await client.GetAsync("/api/clients/by-phone/3001234567");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(
            "application/problem+json",
            rejected.Content.Headers.ContentType?.MediaType);
    }
}

public sealed class ClientPhoneLookupApiFactory : WebApplicationFactory<AuthController>
{
    public ClientEntity KnownClient { get; } = CreateKnownClient();

    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ClientPhoneLookupApiFactory()
    {
        foreach (var setting in BuildEnvironment(phoneLookupPermitLimit: 1000))
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
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
            var knownClient = KnownClient;
            var sender = Substitute.For<ISender>();
            sender.Send(Arg.Any<GetClientByPhoneQuery>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var query = call.Arg<GetClientByPhoneQuery>();
                    var normalized = new string(query.PhoneNumber.Where(char.IsDigit).ToArray());
                    if (normalized == "3001234567")
                    {
                        return knownClient;
                    }

                    throw new NotFoundException("Cliente no encontrado.");
                });
            services.RemoveAll<ISender>();
            services.AddSingleton(sender);
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

    internal static ClientEntity CreateKnownClient() =>
        new(
            Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"),
            "1234567890",
            "Calle Falsa 123",
            registrationDate: new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            phoneNumber: "3001234567");

    internal static Dictionary<string, string> BuildEnvironment(int phoneLookupPermitLimit) =>
        new()
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.phone-lookup-tests",
            ["Jwt__Audience"] = "huellitas-api-phone-lookup-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "phone-lookup-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__LoginPermitLimit"] = "1000",
            ["RateLimiting__LoginWindowSeconds"] = "60",
            ["RateLimiting__RegisterPermitLimit"] = "1000",
            ["RateLimiting__RegisterWindowSeconds"] = "60",
            ["RateLimiting__RefreshPermitLimit"] = "1000",
            ["RateLimiting__RefreshWindowSeconds"] = "60",
            ["RateLimiting__TelegramWebhookPermitLimit"] = "1000",
            ["RateLimiting__TelegramWebhookWindowSeconds"] = "60",
            ["RateLimiting__ClientIdentificationLookupPermitLimit"] = "1000",
            ["RateLimiting__ClientIdentificationLookupWindowSeconds"] = "60",
            ["RateLimiting__ClientPhoneLookupPermitLimit"] = $"{phoneLookupPermitLimit}",
            ["RateLimiting__ClientPhoneLookupWindowSeconds"] = "60",
            ["RateLimiting__AppointmentOtpRequestPermitLimit"] = "1000",
            ["RateLimiting__AppointmentOtpRequestWindowSeconds"] = "60",
            ["RateLimiting__AppointmentOtpConfirmPermitLimit"] = "1000",
            ["RateLimiting__AppointmentOtpConfirmWindowSeconds"] = "60"
        };
}

public sealed class ClientPhoneLookupRateLimitedApiFactory : WebApplicationFactory<AuthController>
{
    public const int PhoneLookupPermitLimit = 2;

    private readonly Dictionary<string, string?> originalEnvironment = [];
    private readonly ClientEntity knownClient = ClientPhoneLookupApiFactory.CreateKnownClient();

    public ClientPhoneLookupRateLimitedApiFactory()
    {
        foreach (var setting in ClientPhoneLookupApiFactory.BuildEnvironment(PhoneLookupPermitLimit))
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
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
            var sender = Substitute.For<ISender>();
            sender.Send(Arg.Any<GetClientByPhoneQuery>(), Arg.Any<CancellationToken>())
                .Returns(knownClient);
            services.RemoveAll<ISender>();
            services.AddSingleton(sender);
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
