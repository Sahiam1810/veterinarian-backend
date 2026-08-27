using System.Security.Cryptography;
using System.Text;

namespace Api.Tests.Support;

internal sealed record RsaTestKeys(
    string PrivateKeyPemBase64,
    string PublicKeyPemBase64)
{
    public static RsaTestKeys Create(int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize);

        return new RsaTestKeys(
            Encode(rsa.ExportPkcs8PrivateKeyPem()),
            Encode(rsa.ExportSubjectPublicKeyInfoPem()));
    }

    private static string Encode(string pem) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));
}
