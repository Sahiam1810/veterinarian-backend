using System.Text.Json.Serialization;

namespace Infrastructure.Agent.Http.Contracts;

internal sealed record AgentHttpResponse(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("correlationId")] Guid CorrelationId,
    [property: JsonPropertyName("responseType")] string ResponseType,
    [property: JsonPropertyName("accessRequirement")] string AccessRequirement,
    [property: JsonPropertyName("provider")] string? Provider,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("usage")] AgentHttpTokenUsage? Usage,
    [property: JsonPropertyName("module")] string? Module,
    [property: JsonPropertyName("rag")] AgentHttpRagResponse Rag);

internal sealed record AgentHttpTokenUsage(
    [property: JsonPropertyName("inputTokens")] int? InputTokens,
    [property: JsonPropertyName("outputTokens")] int? OutputTokens);

internal sealed record AgentHttpRagResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("route")] string Route,
    [property: JsonPropertyName("topScore")] double? TopScore,
    [property: JsonPropertyName("globalMatches")] int GlobalMatches,
    [property: JsonPropertyName("conversationMatches")] int ConversationMatches,
    [property: JsonPropertyName("memoryStored")] bool MemoryStored,
    [property: JsonPropertyName("knowledgePublished")] bool KnowledgePublished);
