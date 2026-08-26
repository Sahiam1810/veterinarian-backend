using System.Text;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Options;
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer)) failures.Add("Jwt:Issuer is required.");
        if (string.IsNullOrWhiteSpace(options.Audience)) failures.Add("Jwt:Audience is required.");
        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32) failures.Add("Jwt:SigningKey must contain at least 32 bytes.");
        if (options.AccessTokenMinutes <= 0) failures.Add("Jwt:AccessTokenMinutes must be positive.");
        if (options.RefreshTokenDays <= 0) failures.Add("Jwt:RefreshTokenDays must be positive.");
        if (options.ClockSkewSeconds is < 0 or > 300) failures.Add("Jwt:ClockSkewSeconds must be between 0 and 300.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}