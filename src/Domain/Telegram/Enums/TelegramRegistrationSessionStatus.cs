namespace Domain.Telegram.Enums;

public enum TelegramRegistrationSessionStatus
{
    AwaitingEmail = 0,
    AwaitingOtp = 1,
    AwaitingProfile = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5,
    Blocked = 6
}
