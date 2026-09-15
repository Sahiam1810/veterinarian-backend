using Microsoft.Extensions.Options;

namespace Infrastructure.Chat.Configuration;

public sealed class ChatOptionsValidator : IValidateOptions<ChatOptions>
{
    public ValidateOptionsResult Validate(string? name, ChatOptions options)
    {
        var failures = new List<string>();
        ValidateRequiredGuid(
            options.ResolvedEscalationStatusId,
            "Chat:ResolvedEscalationStatusId must be a non-empty GUID.",
            failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRequiredGuid(string value, string message, ICollection<string> failures)
    {
        if (!Guid.TryParse(value, out var identifier) || identifier == Guid.Empty)
        {
            failures.Add(message);
        }
    }
}
