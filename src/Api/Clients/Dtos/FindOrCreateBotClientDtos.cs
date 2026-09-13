namespace Api.Clients.Dtos;

public sealed record FindOrCreateBotClientRequest(
    string IdentificationNumber,
    string FullName,
    string Email,
    string? PhoneNumber = null,
    long? TelegramUserId = null,
    long? TelegramChatId = null);

public sealed record FindOrCreateBotClientResponse(
    Guid ClientId,
    Guid UserId,
    Guid UserAccountId,
    string IdentificationNumber,
    bool Created,
    string AccessToken);
