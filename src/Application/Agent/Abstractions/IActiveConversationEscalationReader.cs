namespace Application.Agent.Abstractions;

public interface IActiveConversationEscalationReader
{
    Task<bool> HasActiveAsync(
        Guid conversationId,
        CancellationToken cancellationToken);
}
