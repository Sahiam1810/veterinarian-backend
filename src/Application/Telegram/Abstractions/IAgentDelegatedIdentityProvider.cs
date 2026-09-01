using Application.Telegram.Models;

namespace Application.Telegram.Abstractions;

public interface IAgentDelegatedIdentityProvider
{
    AgentDelegatedIdentity GetGuest(long telegramUserId);

    Task<AgentDelegatedIdentity> GetAsync(
        Guid personId,
        CancellationToken cancellationToken);
}
