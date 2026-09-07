using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Api.Tests.ContactVerification;

// Kickoff Etapa 3: esqueleto anónimo; puertos aún no implementados (501).
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
    public async Task ConfirmEmail_WithoutToken_Returns501_WithCatalogCode()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/contact-verification/email/confirm",
            new { SessionId = Guid.NewGuid(), Code = "123456" });

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "ContactVerification.NotImplemented",
            document.RootElement.GetProperty("code").GetString());
    }
}

public sealed class ContactVerificationApiFactory : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ContactVerificationApiFactory()
    {
        var environment = new Dictionary<string, string>
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
            ["Jwt__ClockSkewSeconds"] = "0"
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

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Testing");

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
