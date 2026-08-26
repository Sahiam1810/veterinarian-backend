namespace Api.ProviderModelsAi.Dtos;

public sealed record ProviderModelAiResponseDto(
    Guid Id,
    string NameProviderAi,
    string? BusinessName,
    string? Website,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
