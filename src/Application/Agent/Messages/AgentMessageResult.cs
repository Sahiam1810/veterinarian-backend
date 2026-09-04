namespace Application.Agent.Messages;

public enum AgentAccessRequirement
{
    None,
    IdentityVerification
}

public sealed record AgentMessageResult(
    string? Message,
    Guid ConversationId,
    Guid CorrelationId,
    string ResponseType,
    string? Provider,
    string? Model,
    AgentTokenUsage? Usage,
    string? Module,
    AgentRagResult Rag,
    AgentAccessRequirement AccessRequirement = AgentAccessRequirement.None);
