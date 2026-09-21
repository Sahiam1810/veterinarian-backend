using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Telegram.Configuration;

public sealed partial class TelegramOptionsValidator : IValidateOptions<TelegramOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
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

        if (options.WorkerPollMilliseconds <= 0) failures.Add("Telegram:WorkerPollMilliseconds must be positive.");
        if (options.WorkerConcurrency is < 1 or > 32)
            failures.Add("Telegram:WorkerConcurrency must be between 1 and 32.");
        if (options.ProcessingLeaseSeconds <= 0) failures.Add("Telegram:ProcessingLeaseSeconds must be positive.");
        if (options.MaxProcessingAttempts is < 1 or > 10) failures.Add("Telegram:MaxProcessingAttempts must be between 1 and 10.");
        if (options.DelegatedTokenMinutes is < 1 or > 15) failures.Add("Telegram:DelegatedTokenMinutes must be between 1 and 15.");
        ValidateRequiredGuid(
            options.PendingEscalationStatusId,
            "Telegram:PendingEscalationStatusId must be a non-empty GUID.",
            failures);
        ValidateRequiredGuid(
            options.TextMessageTypeId,
            "Telegram:TextMessageTypeId must be a non-empty GUID.",
            failures);
        ValidateRequiredGuid(
            options.HumanAgentSenderTypeId,
            "Telegram:HumanAgentSenderTypeId must be a non-empty GUID.",
            failures);
        if (options.PrivateAccessAbsoluteTtlHours is < 1 or > 168)
            failures.Add("Telegram:PrivateAccessAbsoluteTtlHours must be between 1 and 168.");
        if (options.PrivateAccessIdleTtlMinutes is < 1 or > 1440)
            failures.Add("Telegram:PrivateAccessIdleTtlMinutes must be between 1 and 1440.");
        if (TimeSpan.FromMinutes(options.PrivateAccessIdleTtlMinutes) >
            TimeSpan.FromHours(options.PrivateAccessAbsoluteTtlHours))
        {
            failures.Add("Telegram:PrivateAccessIdleTtlMinutes cannot exceed the absolute access lifetime.");
        }
        ValidateOtpPepper(options.OtpPepperBase64, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(string value, string key, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value)) failures.Add($"{key} is required.");
    }

    private static void ValidateRequiredGuid(string value, string message, ICollection<string> failures)
    {
        if (!Guid.TryParse(value, out var identifier) || identifier == Guid.Empty)
        {
            failures.Add(message);
        }
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

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex WebhookSecretPattern();
}
