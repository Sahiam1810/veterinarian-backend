public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int GlobalPermitLimit { get; init; }
    public int GlobalWindowSeconds { get; init; }
    public int LoginPermitLimit { get; init; }
    public int LoginWindowSeconds { get; init; }
    public int RefreshPermitLimit { get; init; }
    public int RefreshWindowSeconds { get; init; }
    public int RegisterPermitLimit { get; init; }
    public int RegisterWindowSeconds { get; init; }
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
}
