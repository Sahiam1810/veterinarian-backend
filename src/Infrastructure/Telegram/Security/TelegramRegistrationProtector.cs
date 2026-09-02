using System.Security.Cryptography;
using System.Text;
using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Security;

public sealed class TelegramRegistrationProtector : ITelegramRegistrationProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] key;

    public TelegramRegistrationProtector(string protectionKeyBase64)
    {
        key = Convert.FromBase64String(protectionKeyBase64);
        if (key.Length != 32)
        {
            throw new ArgumentException("La clave de registro debe tener 32 bytes.", nameof(protectionKeyBase64));
        }
    }

    public string GenerateCompletionToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public string HashCompletionToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public string ProtectEmail(string normalizedEmail)
    {
        var plaintext = Encoding.UTF8.GetBytes(normalizedEmail);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var envelope = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(envelope, 0);
        tag.CopyTo(envelope, NonceSize);
        ciphertext.CopyTo(envelope, NonceSize + TagSize);
        return Convert.ToBase64String(envelope);
    }

    public string UnprotectEmail(string protectedEmail)
    {
        var envelope = Convert.FromBase64String(protectedEmail);
        if (envelope.Length <= NonceSize + TagSize)
        {
            throw new CryptographicException("El correo protegido no es válido.");
        }

        var nonce = envelope.AsSpan(0, NonceSize);
        var tag = envelope.AsSpan(NonceSize, TagSize);
        var ciphertext = envelope.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
