using Microsoft.Extensions.Options;

namespace Api.Configuration;

public sealed class RateLimitOptionsValidator
    : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        var values = new[]
        {
            options.GlobalPermitLimit,
            options.GlobalWindowSeconds,
            options.LoginPermitLimit,
            options.LoginWindowSeconds,
            options.RefreshPermitLimit,
            options.RefreshWindowSeconds,
            options.RegisterPermitLimit,
            options.RegisterWindowSeconds,
            options.TelegramWebhookPermitLimit,
            options.TelegramWebhookWindowSeconds,
            options.ClientIdentificationLookupPermitLimit,
            options.ClientIdentificationLookupWindowSeconds,
            options.ClientPhoneLookupPermitLimit,
            options.ClientPhoneLookupWindowSeconds,
            options.AppointmentOtpRequestPermitLimit,
            options.AppointmentOtpRequestWindowSeconds,
            options.AppointmentOtpConfirmPermitLimit,
            options.AppointmentOtpConfirmWindowSeconds
        };

        return values.All(value => value > 0)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("All rate-limit values must be positive.");
    }
}