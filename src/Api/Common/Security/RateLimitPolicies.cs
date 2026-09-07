namespace Api.Common.Security;

public static class RateLimitPolicies
{
    public const string Login = "Login";
    public const string Refresh = "Refresh";
    public const string TelegramWebhook = "TelegramWebhook";
    public const string TelegramRegistration = "TelegramRegistration";
    public const string ClientIdentificationLookup = "ClientIdentificationLookup";
    public const string ClientPhoneLookup = "ClientPhoneLookup";
    public const string AppointmentOtpRequest = "AppointmentOtpRequest";
    public const string AppointmentOtpConfirm = "AppointmentOtpConfirm";
}
