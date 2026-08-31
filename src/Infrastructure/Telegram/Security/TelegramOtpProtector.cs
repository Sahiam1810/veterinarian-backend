using System.Security.Cryptography;
using System.Text;
using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Security;

public sealed class TelegramOtpProtector : ITelegramOtpProtector
{
    private readonly byte[] _pepper;

    public TelegramOtpProtector(string pepperBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pepperBase64);
        try
        {
            _pepper = Convert.FromBase64String(pepperBase64);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                "El pepper OTP debe estar codificado en Base64.",
                nameof(pepperBase64),
                exception);
        }

        if (_pepper.Length < 32)
        {
            throw new ArgumentException(
                "El pepper OTP debe contener al menos 32 bytes.",
                nameof(pepperBase64));
        }
    }

    public GeneratedTelegramOtp Create()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return new GeneratedTelegramOtp(code, Hash("otp", code));
    }

    public bool Verify(string code, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Convert.FromHexString(Hash("otp", code.Trim()));
        return expected.Length == actual.Length &&
               CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public string HashEmail(string normalizedEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        return Hash("email", normalizedEmail.Trim().ToLowerInvariant());
    }

    private string Hash(string purpose, string value)
    {
        using var hmac = new HMACSHA256(_pepper);
        var data = Encoding.UTF8.GetBytes($"{purpose}:{value}");
        return Convert.ToHexStringLower(hmac.ComputeHash(data));
    }
}
