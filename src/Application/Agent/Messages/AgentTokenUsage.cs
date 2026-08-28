namespace Application.Agent.Messages;

public sealed record AgentTokenUsage(
    int? InputTokens,
    int? OutputTokens);
