public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int GlobalPermitLimit { get; init; }
    public int GlobalWindowSeconds { get; init; }
    public int LoginPermitLimit { get; init; }
    public int LoginWindowSeconds { get; init; }
    public int RefreshPermitLimit { get; init; }
    public int RefreshWindowSeconds { get; init; }
    public int TelegramRegistrationPermitLimit { get; init; } = 5;
    public int TelegramRegistrationWindowSeconds { get; init; } = 60;
    public int TelegramWebhookPermitLimit { get; init; }
    public int TelegramWebhookWindowSeconds { get; init; }
    public int ClientIdentificationLookupPermitLimit { get; init; } = 20;
    public int ClientIdentificationLookupWindowSeconds { get; init; } = 60;
    public int ClientPhoneLookupPermitLimit { get; init; } = 20;
    public int ClientPhoneLookupWindowSeconds { get; init; } = 60;
    public int AppointmentOtpRequestPermitLimit { get; init; } = 5;
    public int AppointmentOtpRequestWindowSeconds { get; init; } = 60;
    public int AppointmentOtpConfirmPermitLimit { get; init; } = 10;
    public int AppointmentOtpConfirmWindowSeconds { get; init; } = 60;
    public int ContactEmailRequestPermitLimit { get; init; } = 5;
    public int ContactEmailRequestWindowSeconds { get; init; } = 60;
    public int ContactEmailConfirmPermitLimit { get; init; } = 10;
    public int ContactEmailConfirmWindowSeconds { get; init; } = 60;
    public int BotOwnerRegistrationPermitLimit { get; init; } = 5;
    public int BotOwnerRegistrationWindowSeconds { get; init; } = 60;
}

