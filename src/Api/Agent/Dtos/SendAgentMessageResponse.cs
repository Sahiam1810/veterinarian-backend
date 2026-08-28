namespace Api.Agent.Dtos;

public sealed record SendAgentMessageResponse(
    string? Message,
    Guid ConversationId,
    Guid CorrelationId,
    string ResponseType,
    string? Provider,
    string? Model,
    SendAgentTokenUsageResponse? Usage,
    string? Module,
    SendAgentRagResponse Rag);

public sealed record SendAgentTokenUsageResponse(
    int? InputTokens,
    int? OutputTokens);

public sealed record SendAgentRagResponse(
    string Status,
    string Route,
    double? TopScore,
    int GlobalMatches,
    int ConversationMatches,
    bool MemoryStored,
    bool KnowledgePublished);
