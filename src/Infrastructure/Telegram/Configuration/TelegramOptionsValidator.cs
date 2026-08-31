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

        if (options.LinkCodeTtlMinutes <= 0) failures.Add("Telegram:LinkCodeTtlMinutes must be positive.");
        if (options.WorkerPollMilliseconds <= 0) failures.Add("Telegram:WorkerPollMilliseconds must be positive.");
        if (options.ProcessingLeaseSeconds <= 0) failures.Add("Telegram:ProcessingLeaseSeconds must be positive.");
        if (options.MaxProcessingAttempts is < 1 or > 10) failures.Add("Telegram:MaxProcessingAttempts must be between 1 and 10.");
        if (options.DelegatedTokenMinutes is < 1 or > 15) failures.Add("Telegram:DelegatedTokenMinutes must be between 1 and 15.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(string value, string key, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value)) failures.Add($"{key} is required.");
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex WebhookSecretPattern();
}
