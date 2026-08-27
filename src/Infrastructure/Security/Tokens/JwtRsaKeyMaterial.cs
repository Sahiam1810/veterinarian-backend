using System.Security.Cryptography;
using System.Text;
using Infrastructure.Security.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.Tokens;

public sealed class JwtRsaKeyMaterial : IDisposable
{
    private readonly RSA signingRsa;
    private readonly RSA validationRsa;

    public JwtRsaKeyMaterial(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        signingRsa = ImportRsa(options.Value.PrivateKeyPemBase64);

        try
        {
            validationRsa = ImportRsa(options.Value.PublicKeyPemBase64);
        }
        catch
        {
            signingRsa.Dispose();
            throw;
        }

        SigningKey = new RsaSecurityKey(signingRsa)
        {
            KeyId = options.Value.KeyId
        };
        ValidationKey = new RsaSecurityKey(validationRsa)
        {
            KeyId = options.Value.KeyId
        };
    }

    public RsaSecurityKey SigningKey { get; }

    public RsaSecurityKey ValidationKey { get; }

    public void Dispose()
    {
        signingRsa.Dispose();
        validationRsa.Dispose();
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
}
