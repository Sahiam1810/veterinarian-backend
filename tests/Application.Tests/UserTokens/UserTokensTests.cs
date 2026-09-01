using Xunit;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.Tests.UserTokens;

public sealed class UserTokensTests
{
    private static readonly DateTime ExpiresAt = new(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsExpiredAsOf_is_false_strictly_before_the_expiration_instant()
    {
        var token = new UserTokenEntity(Guid.NewGuid(), "hash", "refresh", ExpiresAt);

        Assert.False(token.IsExpiredAsOf(new FixedTimeProvider(ExpiresAt.AddSeconds(-1))));
    }

    [Fact]
    public void IsExpiredAsOf_is_true_at_the_exact_expiration_instant()
    {
        var token = new UserTokenEntity(Guid.NewGuid(), "hash", "refresh", ExpiresAt);

        Assert.True(token.IsExpiredAsOf(new FixedTimeProvider(ExpiresAt)));
    }

    [Fact]
    public void IsExpiredAsOf_is_true_after_the_expiration_instant()
    {
        var token = new UserTokenEntity(Guid.NewGuid(), "hash", "refresh", ExpiresAt);

        Assert.True(token.IsExpiredAsOf(new FixedTimeProvider(ExpiresAt.AddSeconds(1))));
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }
}
