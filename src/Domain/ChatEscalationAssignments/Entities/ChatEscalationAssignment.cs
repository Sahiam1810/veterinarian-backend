namespace Domain.ChatEscalationAssignments.Entities;

// Asignación de agente humano a un escalamiento de chat.
public sealed class ChatEscalationAssignment
{
    private ChatEscalationAssignment()
    {
    }

    public Guid Id { get; private set; }

    public Guid AgentHumanId { get; private set; }

    public Guid ChatEscalationId { get; private set; }

    public DateTime? AssignedAt { get; private set; }

    public static ChatEscalationAssignment Create(
        Guid agentHumanId,
        Guid chatEscalationId,
        DateTime? assignedAt = null)
    {
        EnsureAgentHumanId(agentHumanId);
        EnsureChatEscalationId(chatEscalationId);

        return new ChatEscalationAssignment
        {
            Id = Guid.NewGuid(),
            AgentHumanId = agentHumanId,
            ChatEscalationId = chatEscalationId,
            AssignedAt = assignedAt ?? DateTime.UtcNow
        };
    }

    public void Update(Guid agentHumanId, DateTime? assignedAt)
    {
        EnsureAgentHumanId(agentHumanId);

        AgentHumanId = agentHumanId;
        AssignedAt = assignedAt;
    }

    private static void EnsureAgentHumanId(Guid agentHumanId)
    {
        if (agentHumanId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del agente humano es obligatorio.",
                nameof(agentHumanId));
        }
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
}
