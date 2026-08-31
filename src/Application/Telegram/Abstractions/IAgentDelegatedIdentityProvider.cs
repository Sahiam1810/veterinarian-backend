using Application.Telegram.Models;

namespace Application.Telegram.Abstractions;

public interface IAgentDelegatedIdentityProvider
{
    Task<AgentDelegatedIdentity> GetAsync(
        Guid personId,
        CancellationToken cancellationToken);
}
