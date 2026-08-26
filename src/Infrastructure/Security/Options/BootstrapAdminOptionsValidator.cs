using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Options;
public sealed class BootstrapAdminOptionsValidator : IValidateOptions<BootstrapAdminOptions>
{
    public ValidateOptionsResult Validate(string? name, BootstrapAdminOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Username)) failures.Add("BootstrapAdmin:Username is required.");
        if (!MailAddress.TryCreate(options.Email, out _)) failures.Add("BootstrapAdmin:Email must be valid.");
        if (!IsStrongPassword(options.Password)) failures.Add("BootstrapAdmin:Password does not satisfy the configured policy.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsStrongPassword(string password) =>
        password.Length >= 12
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(character => !char.IsLetterOrDigit(character));
}