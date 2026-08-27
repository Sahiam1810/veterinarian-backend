using System.Security.Cryptography;
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
        if (string.IsNullOrWhiteSpace(options.PrivateKeyPemBase64)) failures.Add("Jwt:PrivateKeyPemBase64 is required.");
        if (string.IsNullOrWhiteSpace(options.PublicKeyPemBase64)) failures.Add("Jwt:PublicKeyPemBase64 is required.");
        if (string.IsNullOrWhiteSpace(options.KeyId)) failures.Add("Jwt:KeyId is required.");
        if (options.AccessTokenMinutes <= 0) failures.Add("Jwt:AccessTokenMinutes must be positive.");
        if (options.RefreshTokenDays <= 0) failures.Add("Jwt:RefreshTokenDays must be positive.");
        if (options.ClockSkewSeconds is < 0 or > 300) failures.Add("Jwt:ClockSkewSeconds must be between 0 and 300.");

        if (failures.Count == 0)
        {
            ValidateRsaKeyPair(options, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRsaKeyPair(
        JwtOptions options,
        ICollection<string> failures)
    {
        try
        {
            using var privateRsa = ImportRsa(options.PrivateKeyPemBase64);
            using var publicRsa = ImportRsa(options.PublicKeyPemBase64);

            if (privateRsa.KeySize < 2048 || publicRsa.KeySize < 2048)
            {
                failures.Add("Jwt RSA keys must contain at least 2048 bits.");
                return;
            }

            _ = privateRsa.ExportParameters(includePrivateParameters: true);

            var privatePublicParameters = privateRsa.ExportParameters(
                includePrivateParameters: false);
            var publicParameters = publicRsa.ExportParameters(
                includePrivateParameters: false);

            if (!Equal(privatePublicParameters.Modulus, publicParameters.Modulus)
                || !Equal(privatePublicParameters.Exponent, publicParameters.Exponent))
            {
                failures.Add("Jwt RSA public and private keys must form the same key pair.");
            }
        }
        catch (FormatException)
        {
            failures.Add("Jwt RSA keys must be valid Base64-encoded PEM values.");
        }
        catch (ArgumentException)
        {
            failures.Add("Jwt RSA keys must be valid Base64-encoded PEM values.");
        }
        catch (CryptographicException)
        {
            failures.Add("Jwt RSA keys must contain valid public and private RSA material.");
        }
    }

    private static RSA ImportRsa(string pemBase64)
    {
        var pem = Encoding.UTF8.GetString(Convert.FromBase64String(pemBase64));
        var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(pem);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private static bool Equal(byte[]? left, byte[]? right) =>
        left is not null
        && right is not null
        && left.Length == right.Length
        && CryptographicOperations.FixedTimeEquals(left, right);
}
