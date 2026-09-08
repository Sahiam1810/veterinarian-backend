using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.AccountStatements.Controllers;
using Api.Appointments.Controllers;
using Api.Auth.Controllers;
using Api.Clients.Controllers;
using Api.Common.Security;
using Api.Pets.Controllers;
using Api.Tests.Support;
using Api.Vaccinations.Controllers;
using Application.Common.Exceptions;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

// Tarea 5.2 / Etapa 5: rutas JWT del portal dueno retiradas con 410 Gone.
// request-code y confirm-code de MyAppointmentsController siguen OTP anonimo.
public sealed class ClientOnlyPortalSurfaceTests
{
    [Theory]
    [InlineData(typeof(ClientsController), "GetMe")]
    [InlineData(typeof(PetsController), "GetMine")]
    [InlineData(typeof(PetsController), "RegisterMine")]
    [InlineData(typeof(PetsController), "UpdateMine")]
    [InlineData(typeof(AppointmentsController), "GetMine")]
    [InlineData(typeof(AppointmentsController), "GetMineById")]
    [InlineData(typeof(AppointmentsController), "GetBookingOptions")]
    [InlineData(typeof(AppointmentsController), "GetBookingSlots")]
    [InlineData(typeof(AppointmentsController), "CreateMine")]
    [InlineData(typeof(MyAppointmentsController), "CancelMine")]
    [InlineData(typeof(AccountStatementsController), "GetMine")]
    [InlineData(typeof(VaccinationsController), "GetMine")]
    public void Portal_action_is_allow_anonymous_without_ClientOnly(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.DoesNotContain(
            method.GetCustomAttributes<AuthorizeAttribute>(inherit: true),
            a => a.Policy == "ClientOnly");
    }

    [Fact]
    public void No_controller_uses_the_ClientOnly_policy_anymore()
    {
        var controllerAssembly = typeof(ClientsController).Assembly;
        var clientOnlyUsages = controllerAssembly.GetTypes()
            .SelectMany(type => type.GetMethods())
            .SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
            .Where(attribute => attribute.Policy == "ClientOnly");

        Assert.Empty(clientOnlyUsages);
    }
}

// Excepcion explicita del cierre: OTP de citas sigue anonimo y rate-limited.
public sealed class MyAppointmentsOtpSurfaceTests
{
    [Theory]
    [InlineData("RequestCode", RateLimitPolicies.AppointmentOtpRequest)]
    [InlineData("ConfirmCode", RateLimitPolicies.AppointmentOtpConfirm)]
    public void Otp_action_stays_anonymous_and_rate_limited(string methodName, string expectedRateLimitPolicy)
    {
        var method = typeof(MyAppointmentsController).GetMethod(methodName);
        Assert.NotNull(method);

        Assert.NotEmpty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        Assert.Empty(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));

        var rateLimit = method.GetCustomAttributes<EnableRateLimitingAttribute>(inherit: true).SingleOrDefault();
        Assert.NotNull(rateLimit);
        Assert.Equal(expectedRateLimitPolicy, rateLimit.PolicyName);
    }

    [Fact]
    public void Controller_has_no_class_level_authorization_that_could_shadow_the_otp_actions()
    {
        Assert.Empty(typeof(MyAppointmentsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
    }

    [Fact]
    public async Task CancelMine_throws_ClientPortalGone()
    {
        var controller = new MyAppointmentsController(Substitute.For<ISender>());
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.CancelMine(Guid.NewGuid(), null, CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }
}

public sealed class ClientOnlyPortalClosureHttpTests : IClassFixture<ClientOnlyPortalClosureApiFactory>
{
    private readonly ClientOnlyPortalClosureApiFactory factory;

    public ClientOnlyPortalClosureHttpTests(ClientOnlyPortalClosureApiFactory factory)
    {
        this.factory = factory;
        factory.Sender.ClearReceivedCalls();
    }

    [Theory]
    [InlineData("/api/clients/me")]
    [InlineData("/api/pets/mine")]
    [InlineData("/api/appointments/mine")]
    [InlineData("/api/appointments/booking/options")]
    [InlineData("/api/vaccinations/mine")]
    [InlineData("/api/accountstatements/mine")]
    public async Task Client_jwt_does_not_get_200_on_retired_portal_routes(string path)
    {
        using var client = factory.CreateClientJwtClient();

        using var response = await client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RequestCode_without_jwt_is_not_rejected_as_unauthorized()
    {
        using var client = factory.CreateGuestClient();
        var appointmentId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            $"/api/appointments/mine/{appointmentId}/request-code",
            new { phoneNumber = "3001234567", action = "Cancel" });

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmCode_without_jwt_is_not_rejected_as_unauthorized()
    {
        using var client = factory.CreateGuestClient();
        var appointmentId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            $"/api/appointments/mine/{appointmentId}/confirm-code",
            new { phoneNumber = "3001234567", code = "123456", action = "Cancel" });

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class ClientOnlyPortalClosureApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.ClientOnlyClosure.Tests";
    private const string Audience = "Veterinaria.Client.ClientOnlyClosure.Tests";
    private const string KeyId = "client-only-closure-http-test-key";
    private static readonly Guid ClientRoleId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PersonId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();

    public ClientOnlyPortalClosureApiFactory()
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

        Sender.Send(Arg.Any<IRequest<Guid>>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
    }

    public HttpClient CreateGuestClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public HttpClient CreateClientJwtClient()
    {
        var client = CreateGuestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken());
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

    private string CreateToken()
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
                new Claim("person_id", PersonId.ToString()),
                new Claim("role_id", ClientRoleId.ToString()),
                new Claim(ClaimTypes.Role, "Cliente"),
                new Claim("role", "Cliente")
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}