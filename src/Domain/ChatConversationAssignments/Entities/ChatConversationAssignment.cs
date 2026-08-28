namespace Domain.ChatConversationAssignments.Entities;

// Asignación de agente humano a una conversación (relación 1:1 con la conversación).
public sealed class ChatConversationAssignment
{
    private ChatConversationAssignment()
    {
    }

    public Guid ChatConversationId { get; private set; }

    public Guid? AgentHumanId { get; private set; }

    public DateTime? AssignedAt { get; private set; }

    public DateTime? UnassignedAt { get; private set; }

    // Registra la asignación inicial de una conversación.
    public static ChatConversationAssignment Create(
        Guid chatConversationId,
        Guid? agentHumanId = null,
        DateTime? assignedAt = null)
    {
        EnsureChatConversationId(chatConversationId);
        EnsureAgentHumanId(agentHumanId);

        return new ChatConversationAssignment
        {
            ChatConversationId = chatConversationId,
            AgentHumanId = agentHumanId,
            AssignedAt = agentHumanId.HasValue
                ? assignedAt ?? DateTime.UtcNow
                : assignedAt,
            UnassignedAt = null
        };
    }

    // Asigna un agente humano a la conversación.
    public void Assign(Guid agentHumanId, DateTime? assignedAt = null)
    {
        EnsureAgentHumanId(agentHumanId);

        AgentHumanId = agentHumanId;
        AssignedAt = assignedAt ?? DateTime.UtcNow;
        UnassignedAt = null;
    }

    // Retira la asignación del agente humano.
    public void Unassign(DateTime? unassignedAt = null)
    {
        AgentHumanId = null;
        UnassignedAt = unassignedAt ?? DateTime.UtcNow;
    }

    // Actualiza los datos de asignación.
    public void Update(Guid? agentHumanId, DateTime? assignedAt, DateTime? unassignedAt)
    {
        EnsureAgentHumanId(agentHumanId);

        AgentHumanId = agentHumanId;
        AssignedAt = assignedAt;
        UnassignedAt = unassignedAt;
    }

    private static void EnsureChatConversationId(Guid chatConversationId)
    {
        if (chatConversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la conversación es obligatorio.",
                nameof(chatConversationId));
        }
    }

    private static void EnsureAgentHumanId(Guid? agentHumanId)
    {
        if (agentHumanId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del agente humano no puede ser vacío.",
                nameof(agentHumanId));
        }
    }
}
