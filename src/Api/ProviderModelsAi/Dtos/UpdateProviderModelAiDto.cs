namespace Api.ProviderModelsAi.Dtos;

public sealed record UpdateProviderModelAiDto(
    string NameProviderAi,
    string? BusinessName,
    string? Website);
