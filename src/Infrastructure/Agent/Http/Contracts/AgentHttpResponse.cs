using System.Text.Json.Serialization;

namespace Infrastructure.Agent.Http.Contracts;

internal sealed record AgentHttpResponse(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("correlationId")] Guid CorrelationId,
    [property: JsonPropertyName("responseType")] string ResponseType,
    [property: JsonPropertyName("module")] string? Module);
