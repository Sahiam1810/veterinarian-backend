using Microsoft.Extensions.Options;

namespace Infrastructure.Email.Configuration;

public sealed class EmailVerificationOptionsValidator : IValidateOptions<EmailVerificationOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailVerificationOptions options)
    {
        var failures = new List<string>();

        if (options.OtpTtlMinutes <= 0)
        {
            failures.Add($"{EmailVerificationOptions.SectionName}:OtpTtlMinutes debe ser un entero positivo.");
        }

        if (options.OtpMaximumAttempts <= 0)
        {
            failures.Add($"{EmailVerificationOptions.SectionName}:OtpMaximumAttempts debe ser un entero positivo.");
        }

        if (options.OtpResendSeconds < 0)
        {
            failures.Add($"{EmailVerificationOptions.SectionName}:OtpResendSeconds debe ser un entero no negativo.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
