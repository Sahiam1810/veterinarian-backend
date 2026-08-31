namespace Domain.ChatEscalationResolutions.Entities;

// Resolución de un escalamiento de chat.
public sealed class ChatEscalationResolution
{
    private ChatEscalationResolution()
    {
    }

    public Guid Id { get; private set; }

    public Guid ChatEscalationId { get; private set; }

    public Guid? ResolvedBy { get; private set; }

    public string? ResolutionNote { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public static ChatEscalationResolution Create(
        Guid chatEscalationId,
        Guid? resolvedBy = null,
        string? resolutionNote = null,
        DateTime? resolvedAt = null)
    {
        EnsureChatEscalationId(chatEscalationId);
        EnsureResolvedBy(resolvedBy);

        return new ChatEscalationResolution
        {
            Id = Guid.NewGuid(),
            ChatEscalationId = chatEscalationId,
            ResolvedBy = resolvedBy,
            ResolutionNote = resolutionNote,
            ResolvedAt = resolvedAt ?? DateTime.UtcNow
        };
    }

    public void Update(Guid? resolvedBy, string? resolutionNote, DateTime? resolvedAt)
    {
        EnsureResolvedBy(resolvedBy);

        ResolvedBy = resolvedBy;
        ResolutionNote = resolutionNote;
        ResolvedAt = resolvedAt;
    }

    private static void EnsureChatEscalationId(Guid chatEscalationId)
    {
        if (chatEscalationId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del escalamiento es obligatorio.",
                nameof(chatEscalationId));
        }
    }

    private static void EnsureResolvedBy(Guid? resolvedBy)
    {
        if (resolvedBy == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de quien resuelve no puede ser vacío.",
                nameof(resolvedBy));
        }
    }
}
