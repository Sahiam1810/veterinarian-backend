using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Infrastructure.Agent.Configuration;
using Infrastructure.Agent.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Agent.Http;

public sealed class AgentMessagingHttpClientTests
{
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CorrelationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Send_posts_expected_fastapi_contract_and_maps_safe_result()
    {
        var handler = Respond(HttpStatusCode.OK, SuccessJson());
        var client = CreateClient(handler);

        var result = await client.SendAsync(Envelope(), "secret-token", default);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/api/v1/messages", handler.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.Authorization!.Scheme);
        Assert.Equal("secret-token", handler.Authorization.Parameter);
        Assert.Equal("Respuesta", result.Message);
        Assert.Equal(ConversationId, result.ConversationId);
        Assert.Equal(CorrelationId, result.CorrelationId);
        Assert.Equal("ai_generated", result.ResponseType);
        Assert.DoesNotContain("provider", JsonSerializer.Serialize(result));

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal(11, root.EnumerateObject().Count());
        Assert.Equal("Pregunta", root.GetProperty("message").GetString());
        Assert.Equal(ConversationId, root.GetProperty("conversationId").GetGuid());
        Assert.Equal(PersonId, root.GetProperty("userId").GetGuid());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("petId").ValueKind);
        Assert.Equal("web", root.GetProperty("channel").GetString());
        Assert.Equal("es-CO", root.GetProperty("language").GetString());
        Assert.Equal("Cliente", root.GetProperty("roles")[0].GetString());
        Assert.False(root.GetProperty("isEscalated").GetBoolean());
        Assert.Equal(CorrelationId, root.GetProperty("correlationId").GetGuid());
        Assert.Equal("message-001", root.GetProperty("idempotencyKey").GetString());
        Assert.False(root.GetProperty("publishAsGlobalKnowledge").GetBoolean());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(AgentAuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(AgentAuthenticationException))]
    [InlineData(HttpStatusCode.Conflict, typeof(AgentIdempotencyConflictException))]
    [InlineData(HttpStatusCode.BadRequest, typeof(AgentContractException))]
    [InlineData(HttpStatusCode.UnprocessableEntity, typeof(AgentContractException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(AgentUnavailableException))]
    [InlineData(HttpStatusCode.BadGateway, typeof(AgentUnavailableException))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(AgentUnavailableException))]
    [InlineData(HttpStatusCode.GatewayTimeout, typeof(AgentTimeoutException))]
    public async Task Send_translates_downstream_status_without_exposing_body(
        HttpStatusCode status,
        Type expectedException)
    {
        var client = CreateClient(Respond(status, "sensitive downstream body"));

        var exception = await Record.ExceptionAsync(
            () => client.SendAsync(Envelope(), "secret-token", default));

        Assert.IsType(expectedException, exception);
        Assert.DoesNotContain("sensitive", exception!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-token", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_rejects_invalid_success_json()
    {
        var client = CreateClient(Respond(HttpStatusCode.OK, "not-json"));

        await Assert.ThrowsAsync<AgentContractException>(
            () => client.SendAsync(Envelope(), "secret-token", default));
    }

    [Theory]
    [InlineData("conversationId", "44444444-4444-4444-4444-444444444444")]
    [InlineData("correlationId", "44444444-4444-4444-4444-444444444444")]
    public async Task Send_rejects_mismatched_identifiers(string property, string replacement)
    {
        var json = SuccessJson().Replace(
            $"\"{property}\":\"{(property == "conversationId" ? ConversationId : CorrelationId)}\"",
            $"\"{property}\":\"{replacement}\"",
            StringComparison.Ordinal);
        var client = CreateClient(Respond(HttpStatusCode.OK, json));

        await Assert.ThrowsAsync<AgentContractException>(
            () => client.SendAsync(Envelope(), "secret-token", default));
    }

    [Fact]
    public async Task Send_rejects_response_above_configured_limit()
    {
        var handler = Respond(HttpStatusCode.OK, new string('x', 33));
        var client = CreateClient(handler, maxResponseBytes: 32);

        await Assert.ThrowsAsync<AgentContractException>(
            () => client.SendAsync(Envelope(), "secret-token", default));
    }

    [Fact]
    public async Task Send_translates_http_request_exception_to_unavailable()
    {
        var handler = new RecordingHttpMessageHandler(
            (_, _) => throw new HttpRequestException("internal address"));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AgentUnavailableException>(
            () => client.SendAsync(Envelope(), "secret-token", default));

        Assert.DoesNotContain("internal address", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_translates_timeout_not_requested_by_caller()
    {
        var handler = new RecordingHttpMessageHandler(
            (_, _) => throw new TaskCanceledException("transport timeout"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<AgentTimeoutException>(
            () => client.SendAsync(Envelope(), "secret-token", default));
    }

    [Fact]
    public async Task Send_propagates_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var handler = new RecordingHttpMessageHandler(
            (_, token) => throw new OperationCanceledException(token));
        var client = CreateClient(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.SendAsync(Envelope(), "secret-token", cancellation.Token));
    }

    private static AgentMessagingHttpClient CreateClient(
        RecordingHttpMessageHandler handler,
        int maxResponseBytes = 1_048_576)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://agent-api:8000"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        return new AgentMessagingHttpClient(
            httpClient,
            Options.Create(new AgentOptions
            {
                Enabled = true,
                BaseUrl = "http://agent-api:8000",
                MessagesPath = "/api/v1/messages",
                RequestTimeoutSeconds = 30,
                MaxResponseBytes = maxResponseBytes
            }));
    }

    private static RecordingHttpMessageHandler Respond(HttpStatusCode status, string body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));

    private static AgentMessageEnvelope Envelope() =>
        new(
            "Pregunta",
            ConversationId,
            PersonId,
            null,
            "web",
            "es-CO",
            ["Cliente"],
            false,
            CorrelationId,
            "message-001",
            false);

    private static string SuccessJson() => $$"""
        {
          "message":"Respuesta",
          "conversationId":"{{ConversationId}}",
          "correlationId":"{{CorrelationId}}",
          "responseType":"ai_generated",
          "provider":"openai",
          "model":"gpt-4o-mini",
          "usage":{"inputTokens":10,"outputTokens":5},
          "module":null,
          "rag":{"status":"empty"}
        }
        """;

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public AuthenticationHeaderValue? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return await responder(request, cancellationToken);
        }
    }
}
