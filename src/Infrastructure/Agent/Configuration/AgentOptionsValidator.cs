using Microsoft.Extensions.Options;

namespace Infrastructure.Agent.Configuration;

public sealed class AgentOptionsValidator : IValidateOptions<AgentOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        ValidateBaseUrl(options.BaseUrl, failures);
        ValidateMessagesPath(options.MessagesPath, failures);
        ValidateRange(
            options.RequestTimeoutSeconds,
            1,
            120,
            "Agent:RequestTimeoutSeconds must be between 1 and 120.",
            failures);
        ValidateRange(
            options.MaxResponseBytes,
            1024,
            1_048_576,
            "Agent:MaxResponseBytes must be between 1024 and 1048576.",
            failures);
        ValidateRequiredGuid(
            options.InitialConversationStatusId,
            "Agent:InitialConversationStatusId must be a non-empty GUID.",
            failures);
        ValidateRequiredGuid(
            options.ClientParticipantTypeId,
            "Agent:ClientParticipantTypeId must be a non-empty GUID.",
            failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateBaseUrl(string baseUrl, ICollection<string> failures)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            failures.Add("Agent:BaseUrl must be an absolute safe HTTP or HTTPS URL.");
        }
    }

    private static void ValidateMessagesPath(string messagesPath, ICollection<string> failures)
    {
        var hasTraversal = messagesPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment == "..");
        if (string.IsNullOrWhiteSpace(messagesPath) ||
            !messagesPath.StartsWith("/", StringComparison.Ordinal) ||
            messagesPath.StartsWith("//", StringComparison.Ordinal) ||
            Uri.TryCreate(messagesPath, UriKind.Absolute, out _) ||
            hasTraversal ||
            messagesPath.Contains("?", StringComparison.Ordinal) ||
            messagesPath.Contains("#", StringComparison.Ordinal))
        {
            failures.Add("Agent:MessagesPath must be a safe relative path starting with '/'.");
        }
    }

    private static void ValidateRange(
        int value,
        int minimum,
        int maximum,
        string message,
        ICollection<string> failures)
    {
        if (value < minimum || value > maximum)
        {
            failures.Add(message);
        }
    }

    private static void ValidateRequiredGuid(
        string value,
        string message,
        ICollection<string> failures)
    {
        if (!Guid.TryParse(value, out var identifier) || identifier == Guid.Empty)
        {
            failures.Add(message);
        }
    }
}
