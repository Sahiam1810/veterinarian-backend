namespace Application.Agent.Errors;

public abstract class AgentGatewayException : Exception
{
    protected AgentGatewayException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class AgentUnavailableException(Exception? innerException = null)
    : AgentGatewayException("Agent service is unavailable.", innerException);

public sealed class AgentTimeoutException(Exception? innerException = null)
    : AgentGatewayException("Agent service timed out.", innerException);

public sealed class AgentContractException(Exception? innerException = null)
    : AgentGatewayException("Agent service returned an invalid contract.", innerException);

public sealed class AgentAuthenticationException(Exception? innerException = null)
    : AgentGatewayException("Agent service rejected backend authentication.", innerException);

public sealed class AgentIdempotencyConflictException()
    : AgentGatewayException("Agent idempotency key conflicts with another request.");

public sealed class AgentConversationNotFoundException()
    : AgentGatewayException("Conversation was not found.");

public sealed class AgentConversationForbiddenException()
    : AgentGatewayException("Authenticated user is not a participant in this conversation.");

public sealed class AgentConversationConfigurationException()
    : AgentGatewayException("Conversation catalogs are not configured.");
