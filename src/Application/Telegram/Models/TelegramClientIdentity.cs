namespace Application.Telegram.Models;

public sealed record TelegramClientIdentity(
    Guid PersonId,
    Guid UserAccountId,
    string Email);

public sealed record TelegramClientRegistration(
    string IdentificationNumber,
    string FullName,
    string Email);
