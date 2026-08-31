using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Agent.Dtos;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Api.Tests.Agent;

public sealed class AgentMessagesHttpTests : IClassFixture<AgentApiFactory>
{
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CorrelationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly AgentApiFactory factory;

    public AgentMessagesHttpTests(AgentApiFactory factory)
    {
        this.factory = factory;
        factory.AgentClient.ExceptionToThrow = null;
        factory.AgentClient.ResultToReturn = null;
        factory.ConversationContext.ExceptionToThrow = null;
    }

    [Fact]
    public async Task Post_derives_identity_forwards_token_and_returns_complete_response()
    {
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest(correlationId: CorrelationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AgentApiFactory.PersonId, factory.AgentClient.Envelope!.UserId);
        Assert.Equal(["Cliente"], factory.AgentClient.Envelope.Roles);
        Assert.Equal(factory.AccessToken, factory.AgentClient.AccessToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;
        Assert.Equal(ConversationId, body.GetProperty("conversationId").GetGuid());
        Assert.Equal(CorrelationId, body.GetProperty("correlationId").GetGuid());
        Assert.Equal("openrouter", body.GetProperty("provider").GetString());
        Assert.Equal("google/gemini-flash", body.GetProperty("model").GetString());
        Assert.Equal(12, body.GetProperty("usage").GetProperty("inputTokens").GetInt32());
        Assert.Equal(7, body.GetProperty("usage").GetProperty("outputTokens").GetInt32());
        Assert.Equal("appointments", body.GetProperty("module").GetString());
        var rag = body.GetProperty("rag");
        Assert.Equal("disabled", rag.GetProperty("status").GetString());
        Assert.Equal("direct", rag.GetProperty("route").GetString());
        Assert.Equal(-1, rag.GetProperty("topScore").GetDouble());
        Assert.Equal(0, rag.GetProperty("globalMatches").GetInt32());
        Assert.Equal(0, rag.GetProperty("conversationMatches").GetInt32());
        Assert.True(rag.GetProperty("memoryStored").GetBoolean());
        Assert.True(rag.GetProperty("knowledgePublished").GetBoolean());
    }

    [Fact]
    public async Task Post_without_token_returns_unauthorized_problem()
    {
        using var client = factory.CreateClient(ClientOptions());
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_without_person_id_returns_unauthorized()
    {
        using var client = factory.CreateAuthenticatedClient(includePersonId: false);
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_without_role_returns_unauthorized()
    {
        using var client = factory.CreateAuthenticatedClient(includeRole: false);
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_without_transport_headers_generates_identifiers()
    {
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest(includeIdempotencyKey: false);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches("^msg-[0-9a-f]{32}$", factory.AgentClient.Envelope!.IdempotencyKey);
        Assert.NotEqual(Guid.Empty, factory.AgentClient.Envelope.CorrelationId);
    }

    [Fact]
    public async Task Post_with_transport_headers_preserves_identifiers()
    {
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest(correlationId: CorrelationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("message-001", factory.AgentClient.Envelope!.IdempotencyKey);
        Assert.Equal(CorrelationId, factory.AgentClient.Envelope.CorrelationId);
    }

    [Fact]
    public void OpenApi_describes_transport_headers_as_optional()
    {
        var descriptions = factory.Services
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items);
        var endpoint = Assert.Single(
            descriptions,
            description => description.HttpMethod == HttpMethod.Post.Method &&
                           description.RelativePath == "api/agent/messages");

        var idempotency = Assert.Single(
            endpoint.ParameterDescriptions,
            parameter => parameter.Name == "Idempotency-Key");
        var correlation = Assert.Single(
            endpoint.ParameterDescriptions,
            parameter => parameter.Name == "X-Correlation-ID");

        Assert.False(idempotency.IsRequired);
        Assert.False(correlation.IsRequired);
        Assert.Contains(
            endpoint.SupportedResponseTypes,
            response => response.StatusCode == (int)HttpStatusCode.OK &&
                        response.Type == typeof(SendAgentMessageResponse));
        Assert.Contains(
            endpoint.SupportedResponseTypes,
            response => response.StatusCode == (int)HttpStatusCode.Forbidden);
        Assert.Contains(
            endpoint.SupportedResponseTypes,
            response => response.StatusCode == (int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_preserves_nullable_agent_metadata()
    {
        factory.AgentClient.ResultToReturn = new AgentMessageResult(
            null,
            ConversationId,
            CorrelationId,
            "human_controlled",
            null,
            null,
            null,
            null,
            new AgentRagResult("empty", "general", null, 0, 0, false, false));
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest(correlationId: CorrelationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;
        Assert.Equal(JsonValueKind.Null, body.GetProperty("message").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("provider").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("model").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("usage").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("module").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("rag").GetProperty("topScore").ValueKind);
    }

    [Fact]
    public async Task Post_with_invalid_correlation_header_returns_bad_request()
    {
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest();
        request.Headers.Add("X-Correlation-ID", "not-a-guid");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_without_correlation_header_generates_non_empty_id()
    {
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, factory.AgentClient.Envelope!.CorrelationId);
    }

    [Theory]
    [InlineData("userId", "\"11111111-1111-1111-1111-111111111111\"")]
    [InlineData("roles", "[\"Administrador\"]")]
    [InlineData("channel", "\"telegram\"")]
    [InlineData("isEscalated", "true")]
    [InlineData("publishAsGlobalKnowledge", "true")]
    public async Task Post_rejects_identity_or_control_properties_from_body(
        string property,
        string jsonValue)
    {
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRawRequest($$"""
            {
              "message":"Necesito información",
              "conversationId":null,
              "petId":null,
              "language":"es-CO",
              "{{property}}":{{jsonValue}}
            }
            """);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_with_invalid_body_returns_validation_violations()
    {
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRawRequest("""
            {
              "message":"",
              "conversationId":null,
              "petId":null,
              "language":"es-CO"
            }
            """);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(
            document.RootElement.GetProperty("violations").EnumerateArray(),
            violation => violation.GetProperty("field").GetString() == "message");
    }

    [Theory]
    [MemberData(nameof(GatewayErrors))]
    public async Task Post_maps_gateway_errors_to_safe_public_contract(
        Exception exception,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        factory.AgentClient.ExceptionToThrow = exception;
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, document.RootElement.GetProperty("error").GetString());
        Assert.DoesNotContain("secret-token", await response.Content.ReadAsStringAsync());
    }

    public static TheoryData<Exception, HttpStatusCode, string> GatewayErrors => new()
    {
        { new AgentAuthenticationException(), HttpStatusCode.BadGateway, "agent_authentication_error" },
        { new AgentContractException(), HttpStatusCode.BadGateway, "agent_contract_error" },
        { new AgentIdempotencyConflictException(), HttpStatusCode.Conflict, "agent_idempotency_conflict" },
        { new AgentUnavailableException(), HttpStatusCode.ServiceUnavailable, "agent_unavailable" },
        { new AgentTimeoutException(), HttpStatusCode.GatewayTimeout, "agent_timeout" }
    };

    [Theory]
    [MemberData(nameof(ConversationContextErrors))]
    public async Task Post_maps_conversation_context_errors_to_safe_public_contract(
        Exception exception,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        factory.ConversationContext.ExceptionToThrow = exception;
        using var client = factory.CreateAuthenticatedClient();
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, document.RootElement.GetProperty("error").GetString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(AgentApiFactory.PersonId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ConversationId.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<Exception, HttpStatusCode, string> ConversationContextErrors => new()
    {
        {
            new AgentConversationNotFoundException(),
            HttpStatusCode.NotFound,
            "agent_conversation_not_found"
        },
        {
            new AgentConversationForbiddenException(),
            HttpStatusCode.Forbidden,
            "agent_conversation_forbidden"
        },
        {
            new AgentConversationConfigurationException(),
            HttpStatusCode.ServiceUnavailable,
            "agent_conversation_configuration_error"
        }
    };

    private static HttpRequestMessage CreateRequest(
        bool includeIdempotencyKey = true,
        Guid? correlationId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/agent/messages")
        {
            Content = JsonContent.Create(new
            {
                message = "Necesito información",
                conversationId = (Guid?)null,
                petId = (Guid?)null,
                language = "es-CO"
            })
        };
        if (includeIdempotencyKey)
        {
            request.Headers.Add("Idempotency-Key", "message-001");
        }

        if (correlationId.HasValue)
        {
            request.Headers.Add("X-Correlation-ID", correlationId.Value.ToString());
        }

        return request;
    }

    private static HttpRequestMessage CreateRawRequest(string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/agent/messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", "message-001");
        return request;
    }

    private static WebApplicationFactoryClientOptions ClientOptions() => new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    };
}

public sealed class AgentApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.Agent.Tests";
    private const string Audience = "Veterinaria.Client.Agent.Tests";
    private const string KeyId = "agent-http-test-key";
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private static readonly IReadOnlyDictionary<string, string> TestEnvironment =
        new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = Issuer,
            ["Jwt__Audience"] = Audience,
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = KeyId,
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["Agent__Enabled"] = "false"
        };

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public AgentApiFactory()
    {
        foreach (var setting in TestEnvironment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    public RecordingAgentMessagingClient AgentClient { get; } = new();
    public RecordingConversationContextProvider ConversationContext { get; } =
        new(ConversationId);
    public string AccessToken { get; private set; } = string.Empty;

    public HttpClient CreateAuthenticatedClient(
        bool includePersonId = true,
        bool includeRole = true)
    {
        AccessToken = CreateToken(includePersonId, includeRole);
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AccessToken);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAgentMessagingClient>();
            services.RemoveAll<IConversationContextProvider>();
            services.AddSingleton<IAgentMessagingClient>(AgentClient);
            services.AddSingleton<IConversationContextProvider>(ConversationContext);
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

    private static string CreateToken(bool includePersonId, bool includeRole)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(Convert.FromBase64String(Keys.PrivateKeyPemBase64)));
        var key = new RsaSecurityKey(rsa)
        {
            KeyId = KeyId,
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, AccountId.ToString())
        };
        if (includePersonId)
        {
            claims.Add(new Claim("person_id", PersonId.ToString()));
        }

        if (includeRole)
        {
            claims.Add(new Claim("role", "Cliente"));
        }

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}

public sealed class RecordingConversationContextProvider(Guid conversationId)
    : IConversationContextProvider
{
    public Exception? ExceptionToThrow { get; set; }

    public ValueTask<AgentConversationContext> ResolveAsync(
        Guid personId,
        Guid? requestedConversationId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        ExceptionToThrow is null
            ? ValueTask.FromResult(new AgentConversationContext(
                requestedConversationId ?? conversationId,
                "web",
                false))
            : ValueTask.FromException<AgentConversationContext>(ExceptionToThrow);
}

public sealed class RecordingAgentMessagingClient : IAgentMessagingClient
{
    public AgentMessageEnvelope? Envelope { get; private set; }
    public string? AccessToken { get; private set; }
    public Exception? ExceptionToThrow { get; set; }
    public AgentMessageResult? ResultToReturn { get; set; }

    public Task<AgentMessageResult> SendAsync(
        AgentMessageEnvelope message,
        string accessToken,
        CancellationToken cancellationToken)
    {
        Envelope = message;
        AccessToken = accessToken;
        if (ExceptionToThrow is not null)
        {
            return Task.FromException<AgentMessageResult>(ExceptionToThrow);
        }

        return Task.FromResult(ResultToReturn ?? new AgentMessageResult(
            "Respuesta",
            message.ConversationId,
            message.CorrelationId,
            "ai_generated",
            "openrouter",
            "google/gemini-flash",
            new AgentTokenUsage(12, 7),
            "appointments",
            new AgentRagResult("disabled", "direct", -1, 0, 0, true, true)));
    }
}
