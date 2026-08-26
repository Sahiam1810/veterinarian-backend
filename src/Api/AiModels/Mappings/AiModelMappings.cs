using Api.AiModels.Dtos;
using Application.AiModels.UseCase;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Api.AiModels.Mappings;

public static class AiModelMappings
{
    public static CreateAiModelCommand ToCommand(this CreateAiModelDto dto)
        => new(
            dto.ProviderModelAiId,
            dto.NameModel,
            dto.ModelKey,
            dto.InputTokenPrice,
            dto.OutputTokenPrice,
            dto.MaxTokens,
            dto.ContextWindow);

    public static UpdateAiModelCommand ToCommand(this UpdateAiModelDto dto, Guid id)
        => new(
            id,
            dto.NameModel,
            dto.ModelKey,
            dto.InputTokenPrice,
            dto.OutputTokenPrice,
            dto.MaxTokens,
            dto.ContextWindow);

    public static AiModelResponseDto ToResponse(this AiModelEntity model)
        => new(
            model.Id,
            model.ProviderModelAiId,
            model.NameModel,
            model.ModelKey,
            model.InputTokenPrice,
            model.OutputTokenPrice,
            model.MaxTokens,
            model.ContextWindow,
            model.IsActive,
            model.CreatedAt,
            model.UpdatedAt);

    public static IReadOnlyCollection<AiModelResponseDto> ToResponse(
        this IReadOnlyCollection<AiModelEntity> models)
        => models.Select(model => model.ToResponse()).ToArray();
}
