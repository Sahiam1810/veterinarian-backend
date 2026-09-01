using Domain.Common;

namespace Domain.UserTokens.Entities;

public sealed class UserTokens : BaseEntity<Guid>
{
    private UserTokens()
    {
    }

    public UserTokens(Guid accountId, string tokenValue, string tokenType, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        TokenValue = tokenValue;
        TokenType = tokenType;
        ExpiresAt = expiresAt;
    }

    public Guid AccountId { get; private set; }

    public string TokenValue { get; private set; } = null!;

    public string TokenType { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired => IsExpiredAsOf(TimeProvider.System);

    public bool IsExpiredAsOf(TimeProvider timeProvider) =>
        timeProvider.GetUtcNow().UtcDateTime >= ExpiresAt;
}
