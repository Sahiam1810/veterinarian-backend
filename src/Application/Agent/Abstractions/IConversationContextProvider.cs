using Application.Agent.Messages;

namespace Application.Agent.Abstractions;

public interface IConversationContextProvider
{
    // Ticket B6: "channel" es el canal de origen ("Telegram"/"Web") a persistir
    // en ChatConversation.Channel si esta llamada termina creando una
    // conversación nueva. Si requestedConversationId ya existe, se ignora —
    // el canal de una conversación no cambia después de creada.
    ValueTask<AgentConversationContext> ResolveAsync(
        Guid personId,
        Guid? requestedConversationId,
        string idempotencyKey,
        string channel,
        CancellationToken cancellationToken);
}
