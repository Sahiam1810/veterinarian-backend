using Domain.ChatAiRunMetrics.ValueObjects;
using Domain.Common;

namespace Domain.ChatAiRunMetrics.Entities;

// Entidad de métricas de una ejecución de chat con IA (tabla chat_ai_run_metrics).
public sealed class ChatAiRunMetrics : BaseEntity<Guid>
{
    // Constructor privado para EF Core / materialización.
    private ChatAiRunMetrics()
    {
    }

    // Crea una métrica nueva con Id generado y value objects validados.
    public ChatAiRunMetrics(
        Guid aiRunId,
        int? promptTokens = null,
        int? completionTokens = null,
        int? totalTokens = null,
        decimal? cost = null)
    {
        Id = Guid.NewGuid();
        AiRunId = ChatAiRunId.Create(aiRunId);
        PromptTokens = TokenCount.Create(promptTokens);
        CompletionTokens = TokenCount.Create(completionTokens);
        TotalTokens = TokenCount.Create(totalTokens);
        Cost = MetricCost.Create(cost);
    }

    // Identificador de la ejecución de IA (único en BD, columna ai_run_id).
    public ChatAiRunId AiRunId { get; private set; } = null!;

    // Tokens del prompt (columna prompt_tokens).
    public TokenCount PromptTokens { get; private set; } = null!;

    // Tokens de la respuesta (columna completion_tokens).
    public TokenCount CompletionTokens { get; private set; } = null!;

    // Total de tokens (columna total_tokens).
    public TokenCount TotalTokens { get; private set; } = null!;

    // Costo de la ejecución (columna cost, NUMBER(10,6)).
    public MetricCost Cost { get; private set; } = null!;

    // TODO: habilitar cuando exista la entidad de ai_runs
    // public AiRun AiRun { get; private set; } = null!;

    // Actualiza métricas y marca UpdatedAt en UTC.
    public void Update(
        int? promptTokens,
        int? completionTokens,
        int? totalTokens,
        decimal? cost)
    {
        PromptTokens = TokenCount.Create(promptTokens);
        CompletionTokens = TokenCount.Create(completionTokens);
        TotalTokens = TokenCount.Create(totalTokens);
        Cost = MetricCost.Create(cost);
        UpdatedAt = DateTime.UtcNow;
    }
}
