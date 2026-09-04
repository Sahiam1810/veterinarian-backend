namespace Domain.Telegram.Enums;

public enum TelegramIdentitySessionStatus
{
    AwaitingIdentification,
    AwaitingRegistrationConfirmation,
    AwaitingFullName,
    AwaitingEmail,
    AwaitingOtp,
    Verified,
    Cancelled,
    Expired,
    Blocked
}
