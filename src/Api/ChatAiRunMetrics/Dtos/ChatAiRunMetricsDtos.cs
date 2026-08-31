namespace Api.ChatAiRunMetrics.Dtos;

public sealed record CreateChatAiRunMetricsDto(
    Guid ChatAiRunId,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal Cost);

public sealed record ChatAiRunMetricsResponseDto(
    Guid Id,
    Guid ChatAiRunId,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal Cost,
    DateTime CreatedAt);
