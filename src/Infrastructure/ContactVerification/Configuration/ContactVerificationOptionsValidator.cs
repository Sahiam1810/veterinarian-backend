using Microsoft.Extensions.Options;

namespace Infrastructure.ContactVerification.Configuration;

public sealed class ContactVerificationOptionsValidator
    : IValidateOptions<ContactVerificationOptions>
{
    public ValidateOptionsResult Validate(string? name, ContactVerificationOptions options)
    {
        if (options.OtpTtlMinutes <= 0
            || options.OtpMaximumAttempts <= 0
            || options.OtpResendSeconds <= 0
            || options.ProofTtlMinutes <= 0)
        {
            return ValidateOptionsResult.Fail(
                "ContactVerification TTL, intentos y resend deben ser positivos.");
        }

        return ValidateOptionsResult.Success;
    }
}
