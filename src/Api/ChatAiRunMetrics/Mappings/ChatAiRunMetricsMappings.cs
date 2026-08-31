using Api.ChatAiRunMetrics.Dtos;
using Application.ChatAiRunMetrics.UseCase;
using ChatAiRunMetricsEntity = Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics;

namespace Api.ChatAiRunMetrics.Mappings;

public static class ChatAiRunMetricsMappings
{
    public static CreateChatAiRunMetricsCommand ToCommand(this CreateChatAiRunMetricsDto dto)
        => new(
            dto.ChatAiRunId,
            dto.PromptTokens,
            dto.CompletionTokens,
            dto.TotalTokens,
            dto.Cost);

    public static ChatAiRunMetricsResponseDto ToResponse(this ChatAiRunMetricsEntity metrics)
        => new(
            metrics.Id,
            metrics.ChatAiRunId,
            metrics.PromptTokens,
            metrics.CompletionTokens,
            metrics.TotalTokens,
            metrics.Cost,
            metrics.CreatedAt);
}
