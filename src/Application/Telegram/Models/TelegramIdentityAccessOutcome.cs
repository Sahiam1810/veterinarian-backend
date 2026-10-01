namespace Application.Telegram.Models;

public sealed record TelegramIdentityAccessOutcome(
    bool Consumed,
    string? Reply,
    Guid? VerifiedPersonId = null,
    long? ResumeInboundUpdateId = null,
    string? ResumeMessage = null);
