using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Clients.Abstraction;
using Application.Clients.Errors;
using Application.Common.Abstractions;
using Application.Users.Abstraction;
using Domain.Clients.Entities;
using Domain.Roles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Clients;

// Tarea 2.4: Create/Update phone obligatorio + formato; 400 con code; Create 201.
public sealed class ClientPhoneContractHttpTests : IClassFixture<ClientPhoneContractApiFactory>
{
    private readonly ClientPhoneContractApiFactory factory;

    public ClientPhoneContractHttpTests(ClientPhoneContractApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Post_without_phone_returns_400_with_PhoneRequired_code()
    {
        using var client = factory.CreateAuthenticatedClient();
        var userId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/clients",
            new
            {
                userId,
                identificationNumber = "1234567890",
                address = "Calle Falsa 123"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertViolationCode(await response.Content.ReadAsStringAsync(), ClientErrorCodes.PhoneRequired);
    }

    [Fact]
    public async Task Post_with_invalid_phone_returns_400_with_PhoneInvalidFormat_code()
    {
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/clients",
            new
            {
                userId = Guid.NewGuid(),
                identificationNumber = "1234567890",
                address = "Calle Falsa 123",
                phoneNumber = "123"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertViolationCode(await response.Content.ReadAsStringAsync(), ClientErrorCodes.PhoneInvalidFormat);
    }

    [Fact]
    public async Task Post_with_valid_phone_returns_201_and_normalized_digits()
    {
        using var client = factory.CreateAuthenticatedClient();
        var userId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/clients",
            new
            {
                userId,
                identificationNumber = "1234567890",
                address = "Calle Falsa 123",
                phoneNumber = "+57 (300) 123-4567"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("573001234567", document.RootElement.GetProperty("phoneNumber").GetString());
        Assert.Equal("1234567890", document.RootElement.GetProperty("identificationNumber").GetString());
    }

    [Fact]
    public async Task Put_without_phone_returns_400_with_PhoneRequired_code()
    {
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/clients/{factory.ExistingClientId}",
            new
            {
                userId = factory.ExistingUserId,
                identificationNumber = "1234567890",
                address = "Calle Falsa 123",
                phoneNumber = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertViolationCode(await response.Content.ReadAsStringAsync(), ClientErrorCodes.PhoneRequired);
    }

    [Fact]
    public async Task Put_with_valid_phone_returns_204()
    {
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/clients/{factory.ExistingClientId}",
            new
            {
                userId = factory.ExistingUserId,
                identificationNumber = "1234567890",
                address = "Calle Falsa 123",
                phoneNumber = "+57 301 555 0000"
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static void AssertViolationCode(string json, string expectedCode)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Contains(
            document.RootElement.GetProperty("violations").EnumerateArray(),
            violation => violation.GetProperty("code").GetString() == expectedCode);
    }
}

public sealed class ClientPhoneContractApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.ClientPhoneContract.Tests";
    private const string Audience = "Veterinaria.Client.ClientPhoneContract.Tests";
    private const string KeyId = "client-phone-contract-http-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private ClientEntity? created;

    public Guid ExistingUserId { get; } = Guid.NewGuid();
    public Guid ExistingClientId { get; }

    public ClientPhoneContractApiFactory()
    {
        var existing = new ClientEntity(
            ExistingUserId,
            "1234567890",
            "Calle Falsa 123",
            phoneNumber: "3001234567");
        ExistingClientId = existing.Id;

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

        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        usersRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new UserEntity("Ana", "ana@huellitas.test", "hash", Guid.NewGuid()));

        clientsRepository.ExistsByIdentificationNumberAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);

        clientsRepository.AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                created = call.ArgAt<ClientEntity>(0);
                return Task.CompletedTask;
            });

        clientsRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = call.ArgAt<Guid>(0);
                if (created is not null && created.Id == id)
                {
                    return created;
                }

                return id == existing.Id ? existing : null;
            });

        clientsRepository.UpdateAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateSuperAdminToken());
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

    private static string CreateSuperAdminToken()
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
                new Claim("role_id", SystemRoles.SuperAdminId.ToString())
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
