namespace Application.Agent.Messages;

public sealed record AgentRagResult(
    string Status,
    string Route,
    double? TopScore,
    int GlobalMatches,
    int ConversationMatches,
    bool MemoryStored,
    bool KnowledgePublished);
