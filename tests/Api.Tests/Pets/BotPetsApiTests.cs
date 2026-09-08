using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Auth.Controllers;
using Api.Pets.Dtos;
using Api.Tests.Support;
using Application.Pets.Models;
using Application.Pets.UseCases;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Pets;

public sealed class BotPetsApiTests(TelegramAgentApiFactory factory)
    : IClassFixture<TelegramAgentApiFactory>
{
    [Fact]
    public async Task Ordinary_authenticated_token_cannot_access_bot_pets()
    {
        using var client = factory.CreateJwtClient(tokenUse: null);

        using var response = await client.GetAsync("/api/bot/pets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delegated_token_lists_only_pets_for_its_subject()
    {
        factory.Sender.Send(
                Arg.Is<GetMyPetsQuery>(query => query.UserAccountId == TelegramAgentApiFactory.AccountId),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OwnedPetProfile>());
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.GetAsync("/api/bot/pets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Delegated_token_registers_pet_for_its_subject()
    {
        var speciesId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        var profile = Profile(speciesId, raceId);
        factory.Sender.Send(
                Arg.Is<RegisterMyPetCommand>(command =>
                    command.UserAccountId == TelegramAgentApiFactory.AccountId &&
                    command.SpeciesId == speciesId &&
                    command.RaceId == raceId),
                Arg.Any<CancellationToken>())
            .Returns(profile);
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.PostAsJsonAsync(
            "/api/bot/pets",
            new CreateOwnedPetDto("Luna", 4, "F", 12.5m, null, speciesId, raceId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OwnedPetProfileResponseDto>();
        Assert.Equal(profile.Id, body?.Id);
    }

    [Fact]
    public async Task Delegated_token_with_invalid_subject_is_rejected()
    {
        using var client = factory.CreateJwtClient("telegram_agent", subject: "not-a-guid");

        using var response = await client.GetAsync("/api/bot/pets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delegated_token_updates_only_a_pet_owned_by_its_subject()
    {
        var petId = Guid.NewGuid();
        var speciesId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        var expectedVersion = DateTime.UtcNow.AddMinutes(-1);
        var profile = Profile(speciesId, raceId);
        factory.Sender.Send(
                Arg.Is<UpdateMyPetProfileCommand>(command =>
                    command.UserAccountId == TelegramAgentApiFactory.AccountId &&
                    command.PetId == petId &&
                    command.ExpectedUpdatedAt == expectedVersion),
                Arg.Any<CancellationToken>())
            .Returns(profile);
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.PatchAsJsonAsync(
            $"/api/bot/pets/{petId}",
            new UpdateOwnedPetProfileDto(
                "Luna II",
                null,
                null,
                null,
                null,
                false,
                null,
                null,
                expectedVersion));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OwnedPetProfileResponseDto>();
        Assert.Equal(profile.Id, body?.Id);
    }

    private static OwnedPetProfile Profile(Guid speciesId, Guid raceId) => new(
        Guid.NewGuid(),
        "Luna",
        4,
        "F",
        12.5m,
        null,
        speciesId,
        "Perro",
        raceId,
        "Mestizo",
        DateTime.UtcNow);
}

public sealed class TelegramAgentApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.TelegramAgent.Tests";
    private const string Audience = "Veterinaria.Client.TelegramAgent.Tests";
    private const string KeyId = "telegram-agent-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public ISender Sender { get; } = Substitute.For<ISender>();

    public TelegramAgentApiFactory()
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
    }

    public HttpClient CreateJwtClient(string? tokenUse, string? subject = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(tokenUse, subject));
        return client;
    }

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

    private static string CreateToken(string? tokenUse, string? subject)
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
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject ?? AccountId.ToString()),
            new("person_id", Guid.NewGuid().ToString()),
            new("role_id", Guid.NewGuid().ToString()),
            new("role", "Cliente")
        };
        if (tokenUse is not null)
        {
            claims.Add(new Claim("token_use", tokenUse));
        }

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
