using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging.Configuration;

public sealed class TwilioOptionsValidator : IValidateOptions<TwilioOptions>
{
    public ValidateOptionsResult Validate(string? name, TwilioOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.AccountSid))
        {
            errors.Add("Twilio:AccountSid es obligatorio cuando Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(options.AuthToken))
        {
            errors.Add("Twilio:AuthToken es obligatorio cuando Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(options.FromNumber))
        {
            errors.Add("Twilio:FromNumber es obligatorio cuando Enabled=true.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
