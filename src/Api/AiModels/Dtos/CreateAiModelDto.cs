namespace Api.AiModels.Dtos;

public sealed record CreateAiModelDto(
    Guid ProviderModelAiId,
    string NameModel,
    string ModelKey,
    decimal InputTokenPrice,
    decimal OutputTokenPrice,
    int MaxTokens,
    int ContextWindow);
