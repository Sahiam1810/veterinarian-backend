using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Owners.Controllers;
using Api.Owners.Dtos;
using Api.Tests.Support;
using Application.Clients.Errors;
using Application.Common.Exceptions;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Adapters;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Owners.UseCases;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Owners;

// Etapa 4.2: POST /api/owners/bot anónimo + rate limit. Fakes; sin Oracle/Gmail.
public sealed class BotOwnerRegistrationHttpTests : IClassFixture<BotOwnerRegistrationApiFactory>
{
    private readonly BotOwnerRegistrationApiFactory factory;

    public BotOwnerRegistrationHttpTests(BotOwnerRegistrationApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task Register_WithoutProof_Returns400_AndDoesNotInvokePort()
    {
        factory.ResetRegisterOwner();
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            new
            {
                FullName = "Ana Bot",
                Email = "ana.bot@huellitas.test",
                IdentificationNumber = "1234567890",
                PhoneNumber = "3001234567",
                ContactProofSessionId = Guid.NewGuid(),
                ContactProof = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            OwnerRegistrationErrors.ProofRequired.Code,
            document.RootElement.GetProperty("code").GetString());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        await factory.RegisterOwner.DidNotReceive().RegisterAsync(
            Arg.Any<RegisterOwnerFromBotRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WithEmptySessionId_Returns400_AndDoesNotInvokePort()
    {
        factory.ResetRegisterOwner();
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            new
            {
                FullName = "Ana Bot",
                Email = "ana.bot@huellitas.test",
                IdentificationNumber = "1234567890",
                PhoneNumber = "3001234567",
                ContactProofSessionId = Guid.Empty,
                ContactProof = "proof-token"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            OwnerRegistrationErrors.ProofRequired.Code,
            document.RootElement.GetProperty("code").GetString());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        await factory.RegisterOwner.DidNotReceive().RegisterAsync(
            Arg.Any<RegisterOwnerFromBotRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_ValidProofAndData_Returns201_WithoutSecrets()
    {
        factory.ResetRegisterOwner();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var sessionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        factory.RegisterOwner.RegisterAsync(
                Arg.Any<RegisterOwnerFromBotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new RegisterOwnerResult(userId, clientId));

        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            ValidBody(sessionId, "single-use-proof"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal(userId, document.RootElement.GetProperty("userId").GetGuid());
        Assert.Equal(clientId, document.RootElement.GetProperty("clientId").GetGuid());
        Assert.False(document.RootElement.TryGetProperty("password", out _));
        Assert.False(document.RootElement.TryGetProperty("passwordHash", out _));
        Assert.False(document.RootElement.TryGetProperty("otp", out _));
        Assert.False(document.RootElement.TryGetProperty("proof", out _));
        Assert.False(document.RootElement.TryGetProperty("contactProof", out _));
        Assert.DoesNotContain("single-use-proof", body, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireContactProofs", body, StringComparison.OrdinalIgnoreCase);

        await factory.RegisterOwner.Received(1).RegisterAsync(
            Arg.Is<RegisterOwnerFromBotRequest>(r =>
                r.ContactProofSessionId == sessionId &&
                r.ContactProof == "single-use-proof" &&
                r.Email == "ana.bot@huellitas.test"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("ContactVerification.ProofInvalid", HttpStatusCode.BadRequest)]
    [InlineData("ContactVerification.ProofExpired", HttpStatusCode.Conflict)]
    [InlineData("ContactVerification.ProofAlreadyConsumed", HttpStatusCode.Conflict)]
    public async Task Register_ProofFailures_PreserveCodeAndStatus(
        string errorCode,
        HttpStatusCode expectedStatus)
    {
        var error = errorCode switch
        {
            "ContactVerification.ProofInvalid" => ContactVerificationErrors.ProofInvalid,
            "ContactVerification.ProofExpired" => ContactVerificationErrors.ProofExpired,
            _ => ContactVerificationErrors.ProofAlreadyConsumed
        };
        factory.ResetRegisterOwner();
        factory.RegisterOwner.RegisterAsync(
                Arg.Any<RegisterOwnerFromBotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<RegisterOwnerResult>>(_ =>
                throw new ContactVerificationException(error));

        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            ValidBody(Guid.NewGuid(), "proof-token"));

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(errorCode, document.RootElement.GetProperty("code").GetString());
        Assert.Equal((int)expectedStatus, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
    }

    [Theory]
    [InlineData("Authentication.UserAlreadyExists")]
    [InlineData("Authentication.IdentificationNumberAlreadyExists")]
    [InlineData("Clients.PhoneAlreadyInUse")]
    public async Task Register_Duplicates_Return409_ProblemJson_WithStableCode(string expectedCode)
    {
        factory.ResetRegisterOwner();
        factory.RegisterOwner.RegisterAsync(
                Arg.Any<RegisterOwnerFromBotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<RegisterOwnerResult>>(_ =>
                throw new ConflictException("Owner registration failed.", expectedCode));

        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            ValidBody(Guid.NewGuid(), "proof-token"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(expectedCode, root.GetProperty("code").GetString());
        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.False(root.TryGetProperty("error", out _));
        Assert.False(root.TryGetProperty("userId", out _));
        Assert.False(root.TryGetProperty("clientId", out _));
        Assert.False(root.TryGetProperty("password", out _));
        Assert.False(root.TryGetProperty("passwordHash", out _));
        Assert.False(root.TryGetProperty("otp", out _));
        Assert.False(root.TryGetProperty("proof", out _));
        Assert.False(root.TryGetProperty("contactProof", out _));
        Assert.False(root.TryGetProperty("email", out _));
        Assert.DoesNotContain("ana.bot@huellitas.test", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("proof-token", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ORA-", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_BusinessFailures_AreDecidableByCodeAlone()
    {
        var cases = new (Exception Exception, HttpStatusCode Status, string Code)[]
        {
            (new ContactVerificationException(OwnerRegistrationErrors.ProofRequired),
                HttpStatusCode.BadRequest, OwnerRegistrationErrors.ProofRequired.Code),
            (new ContactVerificationException(ContactVerificationErrors.ProofInvalid),
                HttpStatusCode.BadRequest, ContactVerificationErrors.ProofInvalid.Code),
            (new ContactVerificationException(ContactVerificationErrors.ProofExpired),
                HttpStatusCode.Conflict, ContactVerificationErrors.ProofExpired.Code),
            (new ContactVerificationException(ContactVerificationErrors.ProofAlreadyConsumed),
                HttpStatusCode.Conflict, ContactVerificationErrors.ProofAlreadyConsumed.Code),
            (new ConflictException("dup", AuthenticationErrors.UserAlreadyExists.Code),
                HttpStatusCode.Conflict, AuthenticationErrors.UserAlreadyExists.Code),
            (new ConflictException("dup", AuthenticationErrors.IdentificationNumberAlreadyExists.Code),
                HttpStatusCode.Conflict, AuthenticationErrors.IdentificationNumberAlreadyExists.Code),
            (new ConflictException("dup", ClientErrorCodes.PhoneAlreadyInUse),
                HttpStatusCode.Conflict, ClientErrorCodes.PhoneAlreadyInUse)
        };

        using var client = factory.CreateAnonymousClient();
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (exception, expectedStatus, expectedCode) in cases)
        {
            factory.ResetRegisterOwner();
            factory.RegisterOwner.RegisterAsync(
                    Arg.Any<RegisterOwnerFromBotRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns<Task<RegisterOwnerResult>>(_ => throw exception);

            using var response = await client.PostAsJsonAsync(
                "/api/owners/bot",
                ValidBody(Guid.NewGuid(), "proof-token"));

            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var code = document.RootElement.GetProperty("code").GetString();
            Assert.Equal(expectedCode, code);
            Assert.True(seenCodes.Add(code!), $"code duplicado en contrato bot: {code}");
            Assert.False(document.RootElement.TryGetProperty("error", out _));
        }

        Assert.Equal(cases.Length, seenCodes.Count);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409_WithExistingCode()
    {
        factory.ResetRegisterOwner();
        factory.RegisterOwner.RegisterAsync(
                Arg.Any<RegisterOwnerFromBotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<RegisterOwnerResult>>(_ =>
                throw new ConflictException(
                    "Ya existe un usuario con ese correo electrónico.",
                    OwnerRegistrationErrors.EmailAlreadyInUse.Code));

        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            ValidBody(Guid.NewGuid(), "proof-token"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            AuthenticationErrors.UserAlreadyExists.Code,
            document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_DuplicateIdentification_Returns409_WithExistingCode()
    {
        factory.ResetRegisterOwner();
        factory.RegisterOwner.RegisterAsync(
                Arg.Any<RegisterOwnerFromBotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<RegisterOwnerResult>>(_ =>
                throw new ConflictException(
                    "Ya existe un cliente con ese número de identificación.",
                    OwnerRegistrationErrors.IdentificationAlreadyInUse.Code));

        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            ValidBody(Guid.NewGuid(), "proof-token"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            AuthenticationErrors.IdentificationNumberAlreadyExists.Code,
            document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_DuplicatePhone_Returns409_WithExistingCode()
    {
        factory.ResetRegisterOwner();
        factory.RegisterOwner.RegisterAsync(
                Arg.Any<RegisterOwnerFromBotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<RegisterOwnerResult>>(_ =>
                throw new ConflictException(
                    "Ya existe un cliente con ese número de teléfono.",
                    OwnerRegistrationErrors.PhoneAlreadyInUse.Code));

        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            ValidBody(Guid.NewGuid(), "proof-token"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            ClientErrorCodes.PhoneAlreadyInUse,
            document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public void Register_Endpoint_IsAllowAnonymous()
    {
        var method = typeof(BotOwnersController).GetMethod(nameof(BotOwnersController.Register));
        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void Register_Endpoint_UsesOwnBotOwnerRegistrationRateLimitPolicy()
    {
        var method = typeof(BotOwnersController).GetMethod(nameof(BotOwnersController.Register));
        Assert.NotNull(method);
        var rateLimit = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal(Api.Common.Security.RateLimitPolicies.BotOwnerRegistration, rateLimit!.PolicyName);
    }

    [Fact]
    public void ResponseDto_ExposesOnlyPublicIds_WithoutCredentialSurface()
    {
        var properties = typeof(RegisterOwnerBotResponse)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(["ClientId", "UserId"], properties);
        Assert.Null(typeof(RegisterOwnerBotRequest).GetProperty("RequireContactProofs"));
        Assert.Null(typeof(RegisterOwnerBotRequest).GetProperty("Password"));
        Assert.Null(typeof(RegisterOwnerFromBotRequest).GetProperty("Password"));
    }

    private static object ValidBody(Guid sessionId, string proof) => new
    {
        FullName = "Ana Bot",
        Email = "ana.bot@huellitas.test",
        IdentificationNumber = "1234567890",
        PhoneNumber = "3001234567",
        ContactProofSessionId = sessionId,
        ContactProof = proof
    };
}

// Confirma que el adaptador Bot fija Channel=Bot (RequireContactProofs efectivo en el núcleo).
public sealed class BotOwnerRegistrationChannelContractTests
    : IClassFixture<BotOwnerRegistrationChannelApiFactory>
{
    private readonly BotOwnerRegistrationChannelApiFactory factory;

    public BotOwnerRegistrationChannelContractTests(BotOwnerRegistrationChannelApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task Register_DispatchesBotChannel_WhichAlwaysRequiresContactProof()
    {
        RegisterOwnerCommand? captured = null;
        factory.Sender.Send(Arg.Any<RegisterOwnerCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<RegisterOwnerCommand>();
                return new RegisterOwnerResult(Guid.NewGuid(), Guid.NewGuid());
            });

        using var client = factory.CreateAnonymousClient();
        var sessionId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/owners/bot",
            new
            {
                FullName = "Ana Bot",
                Email = "ana.bot@huellitas.test",
                IdentificationNumber = "1234567890",
                PhoneNumber = "3001234567",
                ContactProofSessionId = sessionId,
                ContactProof = "proof-token",
                RequireContactProofs = false
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(captured);
        Assert.Equal(RegisterOwnerChannel.Bot, captured!.Channel);
        Assert.Equal(sessionId, captured.ContactProofSessionId);
        Assert.Equal("proof-token", captured.ContactProof);
        // Channel Bot ⇒ ConfiguredRegisterOwnerSettings.RequiresContactProof siempre true.
        Assert.True(
            captured.Channel is RegisterOwnerChannel.Bot or RegisterOwnerChannel.Telegram);
    }
}

public class BotOwnerRegistrationApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public IRegisterOwnerFromBot RegisterOwner { get; } = Substitute.For<IRegisterOwnerFromBot>();

    public void ResetRegisterOwner()
    {
        RegisterOwner.ClearReceivedCalls();
        RegisterOwner.RegisterAsync(
                Arg.Any<RegisterOwnerFromBotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new RegisterOwnerResult(Guid.NewGuid(), Guid.NewGuid()));
    }

    public BotOwnerRegistrationApiFactory()
        : this(CreateDefaultEnvironment())
    {
    }

    protected BotOwnerRegistrationApiFactory(Dictionary<string, string> environment)
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
            ["Jwt__Issuer"] = "https://issuer.huellitas.bot-owner-tests",
            ["Jwt__Audience"] = "huellitas-api-bot-owner-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "bot-owner-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["Email__Enabled"] = "false",
            ["Twilio__Enabled"] = "false",
            ["Telegram__Enabled"] = "false",
            ["RegisterOwner__RequireContactProofs"] = "false",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__BotOwnerRegistrationPermitLimit"] = "1000",
            ["RateLimiting__BotOwnerRegistrationWindowSeconds"] = "60"
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
            services.RemoveAll<IRegisterOwnerFromBot>();
            services.AddSingleton(RegisterOwner);
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

public sealed class BotOwnerRegistrationChannelApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();

    public BotOwnerRegistrationChannelApiFactory()
    {
        var environment = new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.bot-owner-channel-tests",
            ["Jwt__Audience"] = "huellitas-api-bot-owner-channel-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "bot-owner-channel-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["Email__Enabled"] = "false",
            ["Twilio__Enabled"] = "false",
            ["Telegram__Enabled"] = "false",
            ["RegisterOwner__RequireContactProofs"] = "false",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__BotOwnerRegistrationPermitLimit"] = "1000",
            ["RateLimiting__BotOwnerRegistrationWindowSeconds"] = "60"
        };

        foreach (var setting in environment)
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
            services.RemoveAll<ISender>();
            services.AddSingleton(Sender);
            services.RemoveAll<IRegisterOwnerFromBot>();
            services.AddScoped<IRegisterOwnerFromBot, RegisterOwnerFromBot>();
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
