namespace Application.Telegram.Models;

public sealed record AgentDelegatedIdentity(
    Guid PersonId,
    string Role,
    string AccessToken);
