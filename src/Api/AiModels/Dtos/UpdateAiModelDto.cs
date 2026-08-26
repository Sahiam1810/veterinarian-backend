namespace Api.AiModels.Dtos;

public sealed record UpdateAiModelDto(
    string NameModel,
    string ModelKey,
    decimal InputTokenPrice,
    decimal OutputTokenPrice,
    int MaxTokens,
    int ContextWindow);
