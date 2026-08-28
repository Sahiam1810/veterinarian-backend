using System.Text.Json.Serialization;

namespace Infrastructure.Agent.Http.Contracts;

internal sealed record AgentHttpRequest(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("petId")] Guid? PetId,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("isEscalated")] bool IsEscalated,
    [property: JsonPropertyName("correlationId")] Guid CorrelationId,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey,
    [property: JsonPropertyName("publishAsGlobalKnowledge")] bool PublishAsGlobalKnowledge);
