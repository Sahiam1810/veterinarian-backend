namespace Infrastructure.Agent.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string MessagesPath { get; init; } = "/api/v1/messages";
    public int RequestTimeoutSeconds { get; init; } = 30;
    public int ConversationContextTtlSeconds { get; init; } = 900;
    public int ConversationContextCapacity { get; init; } = 10_000;
    public int MaxResponseBytes { get; init; } = 1_048_576;
    public string InitialConversationStatusId { get; init; } = string.Empty;
    public string ClientParticipantTypeId { get; init; } = string.Empty;
}
