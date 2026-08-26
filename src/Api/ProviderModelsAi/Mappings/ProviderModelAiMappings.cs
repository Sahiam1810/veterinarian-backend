using Api.ProviderModelsAi.Dtos;
using Application.ProviderModelsAi.UseCase;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Api.ProviderModelsAi.Mappings;

public static class ProviderModelAiMappings
{
    public static CreateProviderModelAiCommand ToCommand(this CreateProviderModelAiDto dto)
        => new(dto.NameProviderAi, dto.BusinessName, dto.Website);

    public static UpdateProviderModelAiCommand ToCommand(this UpdateProviderModelAiDto dto, Guid id)
        => new(id, dto.NameProviderAi, dto.BusinessName, dto.Website);

    public static ProviderModelAiResponseDto ToResponse(this ProviderEntity provider)
        => new(
            provider.Id,
            provider.NameProviderAi,
            provider.BusinessName,
            provider.Website,
            provider.IsActive,
            provider.CreatedAt,
            provider.UpdatedAt);

    public static IReadOnlyCollection<ProviderModelAiResponseDto> ToResponse(
        this IReadOnlyCollection<ProviderEntity> providers)
        => providers.Select(provider => provider.ToResponse()).ToArray();
}
