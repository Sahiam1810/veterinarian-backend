namespace Api.AiModels.Dtos;

public sealed record AiModelResponseDto(
    Guid Id,
    Guid ProviderModelAiId,
    string NameModel,
    string ModelKey,
    decimal InputTokenPrice,
    decimal OutputTokenPrice,
    int MaxTokens,
    int ContextWindow,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
