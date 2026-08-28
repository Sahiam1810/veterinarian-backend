using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Infrastructure.Agent.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Agent.Conversations;

public sealed class TransientConversationContextProvider(
    IOptions<AgentOptions> options,
    TimeProvider timeProvider) : IConversationContextProvider
{
    private readonly object sync = new();
    private readonly Dictionary<ConversationKey, Entry> entries = [];
    private readonly int capacity = options.Value.ConversationContextCapacity;
    private readonly TimeSpan ttl = TimeSpan.FromSeconds(options.Value.ConversationContextTtlSeconds);

    public ValueTask<AgentConversationContext> ResolveAsync(
        Guid personId,
        Guid? requestedConversationId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (requestedConversationId is { } suppliedId)
        {
            return ValueTask.FromResult(new AgentConversationContext(suppliedId, "web", false));
        }

        var key = new ConversationKey(personId, idempotencyKey.Trim());
        lock (sync)
        {
            var now = timeProvider.GetUtcNow();
            foreach (var expired in entries
                         .Where(pair => pair.Value.ExpiresAt <= now)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                entries.Remove(expired);
            }

            if (entries.TryGetValue(key, out var existing))
            {
                return ValueTask.FromResult(
                    new AgentConversationContext(existing.ConversationId, "web", false));
            }

            if (entries.Count >= capacity)
            {
                throw new AgentConversationCapacityException();
            }

            var conversationId = Guid.NewGuid();
            entries.Add(key, new Entry(conversationId, now.Add(ttl)));
            return ValueTask.FromResult(
                new AgentConversationContext(conversationId, "web", false));
        }
    }

    private readonly record struct ConversationKey(Guid PersonId, string IdempotencyKey);
    private sealed record Entry(Guid ConversationId, DateTimeOffset ExpiresAt);
}
