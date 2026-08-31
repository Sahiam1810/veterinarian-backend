namespace Domain.Telegram.Enums;

public enum TelegramLinkingSessionStatus
{
    AwaitingEmail = 0,
    AwaitingOtp = 1,
    Linked = 2,
    Cancelled = 3,
    Expired = 4,
    Blocked = 5
}
