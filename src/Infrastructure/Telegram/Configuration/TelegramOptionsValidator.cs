using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Telegram.Configuration;

public sealed partial class TelegramOptionsValidator : IValidateOptions<TelegramOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramOptions options)
    {
        if (!options.Enabled)
        {
            return options.RegistrationEnabled
                ? ValidateOptionsResult.Fail("Telegram:RegistrationEnabled requires Telegram:Enabled=true.")
                : ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        Require(options.BotToken, "Telegram:BotToken", failures);
        Require(options.BotUsername, "Telegram:BotUsername", failures);
        if (string.IsNullOrWhiteSpace(options.WebhookSecret) ||
            options.WebhookSecret.Length > 256 ||
            !WebhookSecretPattern().IsMatch(options.WebhookSecret))
        {
            failures.Add("Telegram:WebhookSecret must contain 1-256 letters, digits, underscores or hyphens.");
        }

        if (!Uri.TryCreate(options.PublicWebhookUrl, UriKind.Absolute, out var publicUrl) ||
            publicUrl.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("Telegram:PublicWebhookUrl must be an absolute HTTPS URL.");
        }

        if (options.LinkCodeTtlMinutes <= 0) failures.Add("Telegram:LinkCodeTtlMinutes must be positive.");
        if (options.WorkerPollMilliseconds <= 0) failures.Add("Telegram:WorkerPollMilliseconds must be positive.");
        if (options.ProcessingLeaseSeconds <= 0) failures.Add("Telegram:ProcessingLeaseSeconds must be positive.");
        if (options.MaxProcessingAttempts is < 1 or > 10) failures.Add("Telegram:MaxProcessingAttempts must be between 1 and 10.");
        if (options.DelegatedTokenMinutes is < 1 or > 15) failures.Add("Telegram:DelegatedTokenMinutes must be between 1 and 15.");
        if (options.OtpTtlMinutes is < 1 or > 15) failures.Add("Telegram:OtpTtlMinutes must be between 1 and 15.");
        if (options.OtpMaximumAttempts is < 1 or > 10) failures.Add("Telegram:OtpMaximumAttempts must be between 1 and 10.");
        if (options.OtpResendSeconds is < 30 or > 3600) failures.Add("Telegram:OtpResendSeconds must be between 30 and 3600.");
        ValidateOtpPepper(options.OtpPepperBase64, failures);
        if (options.RegistrationEnabled)
        {
            if (!Uri.TryCreate(options.RegistrationCompletionUrl, UriKind.Absolute, out var registrationUrl) ||
                registrationUrl.Scheme != Uri.UriSchemeHttps)
            {
                failures.Add("Telegram:RegistrationCompletionUrl must be an absolute HTTPS URL.");
            }

            if (options.RegistrationOtpTtlMinutes is < 1 or > 15)
                failures.Add("Telegram:RegistrationOtpTtlMinutes must be between 1 and 15.");
            if (options.RegistrationTokenTtlMinutes is < 1 or > 60)
                failures.Add("Telegram:RegistrationTokenTtlMinutes must be between 1 and 60.");
            if (options.RegistrationMaxOtpAttempts is < 1 or > 10)
                failures.Add("Telegram:RegistrationMaxOtpAttempts must be between 1 and 10.");
            if (options.RegistrationResendSeconds is < 30 or > 3600)
                failures.Add("Telegram:RegistrationResendSeconds must be between 30 and 3600.");
            ValidateRegistrationKey(options.RegistrationProtectionKeyBase64, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(string value, string key, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value)) failures.Add($"{key} is required.");
    }

    private static void ValidateOtpPepper(string value, ICollection<string> failures)
    {
        try
        {
            if (Convert.FromBase64String(value).Length >= 32)
            {
                return;
            }
        }
        catch (FormatException)
        {
        }

        failures.Add("Telegram:OtpPepperBase64 must contain at least 32 random bytes encoded as Base64.");
    }

    private static void ValidateRegistrationKey(string value, ICollection<string> failures)
    {
        try
        {
            if (Convert.FromBase64String(value).Length == 32)
            {
                return;
            }
        }
        catch (FormatException)
        {
        }

        failures.Add("Telegram:RegistrationProtectionKeyBase64 must contain exactly 32 random bytes encoded as Base64.");
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex WebhookSecretPattern();
}
