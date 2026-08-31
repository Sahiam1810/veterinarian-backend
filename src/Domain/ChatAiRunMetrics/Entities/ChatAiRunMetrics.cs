using Domain.Common;

namespace Domain.ChatAiRunMetrics.Entities;

/// <summary>
/// Métricas de consumo y costo de una ejecución de IA (inmutable tras creación).
/// </summary>
public sealed class ChatAiRunMetrics : BaseEntity<Guid>
{
    private ChatAiRunMetrics()
    {
    }

    public Guid ChatAiRunId { get; private set; }

    public int PromptTokens { get; private set; }

    public int CompletionTokens { get; private set; }

    public int TotalTokens { get; private set; }

    public decimal Cost { get; private set; }

    /// <summary>
    /// Crea métricas validando tokens no negativos y coherencia del total.
    /// </summary>
    public static ChatAiRunMetrics Create(
        Guid chatAiRunId,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        decimal cost)
    {
        EnsureChatAiRunId(chatAiRunId);
        EnsureNonNegative(promptTokens, nameof(promptTokens));
        EnsureNonNegative(completionTokens, nameof(completionTokens));
        EnsureNonNegative(totalTokens, nameof(totalTokens));
        EnsureNonNegativeCost(cost);

        if (totalTokens != promptTokens + completionTokens)
        {
            throw new ArgumentException(
                "El total de tokens debe ser igual a la suma de tokens de prompt y completado.",
                nameof(totalTokens));
        }

        return new ChatAiRunMetrics
        {
            Id = Guid.NewGuid(),
            ChatAiRunId = chatAiRunId,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens,
            Cost = decimal.Round(cost, 6, MidpointRounding.AwayFromZero)
        };
    }

    private static void EnsureChatAiRunId(Guid chatAiRunId)
    {
        if (chatAiRunId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la ejecución de IA es obligatorio.",
                nameof(chatAiRunId));
        }
    }

    private static void EnsureNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentException(
                "La cantidad de tokens no puede ser negativa.",
                paramName);
        }
    }

    private static void EnsureNonNegativeCost(decimal cost)
    {
        if (cost < 0)
        {
            throw new ArgumentException(
                "El costo no puede ser negativo.",
                nameof(cost));
        }
    }
}
