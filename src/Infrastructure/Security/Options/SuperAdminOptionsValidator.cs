using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Options;

public sealed class SuperAdminOptionsValidator : IValidateOptions<SuperAdminOptions>
{
    public ValidateOptionsResult Validate(string? name, SuperAdminOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.Id == Guid.Empty) failures.Add("SuperAdmin:Id is required.");
        if (!MailAddress.TryCreate(options.Email, out _)) failures.Add("SuperAdmin:Email must be valid.");
        if (!IsValidPasswordHash(options.PasswordHash)) failures.Add("SuperAdmin:PasswordHash must be a valid hash produced by IPasswordHasher.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    // El hash lo produce IPasswordHasher.Hash(...) con el formato "iteraciones.salt.clave".
    // Acá solo se valida la forma, no se puede recalcular sin el password en texto plano.
    private static bool IsValidPasswordHash(string passwordHash)
    {
        var parts = passwordHash.Split('.', 3);
        return parts.Length == 3 && int.TryParse(parts[0], out _);
    }
}
