using Application.Agent.Errors;
using Infrastructure.Agent.Configuration;
using Infrastructure.Agent.Conversations;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Agent.Conversations;

public sealed class TransientConversationContextProviderTests
{
    private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Resolve_without_id_reuses_generated_id_for_same_person_and_key()
    {
        var provider = CreateProvider(capacity: 10, ttlSeconds: 60);

        var first = await provider.ResolveAsync(PersonId, null, "message-001", default);
        var retry = await provider.ResolveAsync(PersonId, null, "message-001", default);

        Assert.NotEqual(Guid.Empty, first.ConversationId);
        Assert.Equal(first.ConversationId, retry.ConversationId);
        Assert.Equal("web", first.Channel);
        Assert.False(first.IsEscalated);
    }

    [Fact]
    public async Task Resolve_uses_person_and_idempotency_as_composite_key()
    {
        var provider = CreateProvider(capacity: 10, ttlSeconds: 60);
        var anotherPerson = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var first = await provider.ResolveAsync(PersonId, null, "message-001", default);
        var differentPerson = await provider.ResolveAsync(anotherPerson, null, "message-001", default);
        var differentKey = await provider.ResolveAsync(PersonId, null, "message-002", default);

        Assert.NotEqual(first.ConversationId, differentPerson.ConversationId);
        Assert.NotEqual(first.ConversationId, differentKey.ConversationId);
    }

    [Fact]
    public async Task Resolve_with_requested_id_returns_it_without_consuming_capacity()
    {
        var provider = CreateProvider(capacity: 1, ttlSeconds: 60);
        var requested = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var resolved = await provider.ResolveAsync(PersonId, requested, "message-001", default);
        var generated = await provider.ResolveAsync(PersonId, null, "message-002", default);

        Assert.Equal(requested, resolved.ConversationId);
        Assert.NotEqual(Guid.Empty, generated.ConversationId);
    }

    [Fact]
    public async Task Resolve_replaces_expired_entry()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var provider = CreateProvider(capacity: 1, ttlSeconds: 60, time);
        var first = await provider.ResolveAsync(PersonId, null, "message-001", default);

        time.Advance(TimeSpan.FromSeconds(61));
        var replacement = await provider.ResolveAsync(PersonId, null, "message-001", default);

        Assert.NotEqual(first.ConversationId, replacement.ConversationId);
    }

    [Fact]
    public async Task Resolve_honors_cancellation_before_mutating_state()
    {
        var provider = CreateProvider(capacity: 1, ttlSeconds: 60);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.ResolveAsync(PersonId, null, "message-001", cancellation.Token).AsTask());

        var resolved = await provider.ResolveAsync(PersonId, null, "message-002", default);
        Assert.NotEqual(Guid.Empty, resolved.ConversationId);
    }

    [Fact]
    public async Task Resolve_throws_when_unexpired_capacity_is_exhausted()
    {
        var provider = CreateProvider(capacity: 1, ttlSeconds: 60);
        await provider.ResolveAsync(PersonId, null, "message-001", default);

        await Assert.ThrowsAsync<AgentConversationCapacityException>(
            () => provider.ResolveAsync(PersonId, null, "message-002", default).AsTask());
    }

    private static TransientConversationContextProvider CreateProvider(
        int capacity,
        int ttlSeconds,
        TimeProvider? timeProvider = null) =>
        new(
            Options.Create(new AgentOptions
            {
                ConversationContextCapacity = capacity,
                ConversationContextTtlSeconds = ttlSeconds
            }),
            timeProvider ?? TimeProvider.System);

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan amount) => utcNow = utcNow.Add(amount);
    }
}
