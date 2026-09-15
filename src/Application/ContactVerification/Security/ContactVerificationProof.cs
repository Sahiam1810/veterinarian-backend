using System.Security.Cryptography;
using System.Text;

namespace Application.ContactVerification.Security;

// Proof criptográficamente aleatorio; solo se persiste el SHA-256 hex.
public static class ContactVerificationProof
{
    private const int ProofByteLength = 32;

    public static string Generate() =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(ProofByteLength));

    public static string Hash(string proof)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proof);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(proof)));
    }

    public static bool Matches(string proof, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(proof) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        byte[] expected;
        byte[] actual;
        try
        {
            expected = Convert.FromHexString(expectedHash);
            actual = Convert.FromHexString(Hash(proof));
        }
        catch (FormatException)
        {
            return false;
        }

        return expected.Length == actual.Length
               && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
