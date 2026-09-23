using Domain.Common;

namespace Domain.ChatConversations.Entities;

/// <summary>
/// Conversación de chat con ciclo de vida básico (estado, prioridad, IA y cierre).
/// </summary>
public sealed class ChatConversation : BaseEntity<Guid>
{
    private ChatConversation()
    {
    }

    public bool AiEnabled { get; private set; }

    public DateTime? LastMessageAt { get; private set; }

    public bool Closed { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public Guid? ClosedBy { get; private set; }

    public string Channel { get; private set; } = "Web";

    /// <summary>
    /// Crea una conversación abierta. La IA queda habilitada por defecto salvo indicación contraria.
    /// </summary>
    public static ChatConversation Create(
        bool aiEnabled = true,
        string channel = "Web")
    {
        EnsureChannel(channel);

        return new ChatConversation
        {
            Id = Guid.NewGuid(),
            AiEnabled = aiEnabled,
            Closed = false,
            ClosedAt = null,
            ClosedBy = null,
            Channel = channel
        };
    }

    /// <summary>
    /// Habilita o deshabilita la IA en la conversación.
    /// </summary>
    public void SetAiEnabled(bool aiEnabled)
    {
        AiEnabled = aiEnabled;
        Touch();
    }

    /// <summary>
    /// Actualiza la fecha del último mensaje.
    /// </summary>
    public void UpdateLastMessageAt(DateTime lastMessageAt)
    {
        LastMessageAt = lastMessageAt;
        Touch();
    }

    /// <summary>
    /// Cierra la conversación con fecha UTC y un identificador técnico opcional.
    /// </summary>
    public void Close(Guid? closedBy = null)
    {
        if (Closed)
        {
            return;
        }

        EnsureClosedBy(closedBy);

        Closed = true;
        ClosedAt = DateTime.UtcNow;
        ClosedBy = closedBy;
        Touch();
    }

    /// <summary>
    /// Reabre la conversación limpiando datos de cierre.
    /// </summary>
    public void Reopen()
    {
        Closed = false;
        ClosedAt = null;
        ClosedBy = null;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureClosedBy(Guid? closedBy)
    {
        if (closedBy == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de cierre no puede ser vacío.",
                nameof(closedBy));
        }
    }

    private static void EnsureChannel(string channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            throw new ArgumentException(
                "El canal de la conversación es obligatorio.",
                nameof(channel));
        }
    }
}
