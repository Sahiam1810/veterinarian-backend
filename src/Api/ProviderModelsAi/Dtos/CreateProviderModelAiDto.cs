namespace Api.ProviderModelsAi.Dtos;

public sealed record CreateProviderModelAiDto(
    string NameProviderAi,
    string? BusinessName,
    string? Website);
