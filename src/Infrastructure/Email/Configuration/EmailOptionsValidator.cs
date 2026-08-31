using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email.Configuration;

public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        Require(options.Host, "Email:Host", failures);
        Require(options.Username, "Email:Username", failures);
        Require(options.Password, "Email:Password", failures);
        Require(options.FromName, "Email:FromName", failures);
        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Email:Port must be between 1 and 65535.");
        }

        if (!MailAddress.TryCreate(options.FromAddress, out _))
        {
            failures.Add("Email:FromAddress must be a valid email address.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(
        string value,
        string key,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required.");
        }
    }
}
