using Api.Tests.Support;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Tests.Security;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator validator = new();

    [Fact]
    public void Validate_accepts_matching_rsa_keys_with_at_least_2048_bits()
    {
        var keys = RsaTestKeys.Create();

        var result = validator.Validate(null, ValidOptions(keys));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_rejects_public_key_from_a_different_pair()
    {
        var privateKeys = RsaTestKeys.Create();
        var publicKeys = RsaTestKeys.Create();
        var options = CreateOptions(
            privateKeys.PrivateKeyPemBase64,
            publicKeys.PublicKeyPemBase64,
            "test-key-2026-08");

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_rejects_rsa_keys_smaller_than_2048_bits()
    {
        var result = validator.Validate(null, ValidOptions(RsaTestKeys.Create(1024)));

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("not-base64", null)]
    [InlineData(null, "not-base64")]
    [InlineData("", null)]
    [InlineData(null, "")]
    public void Validate_rejects_missing_or_invalid_key_material(
        string? privateKey,
        string? publicKey)
    {
        var keys = RsaTestKeys.Create();
        var options = CreateOptions(
            privateKey ?? keys.PrivateKeyPemBase64,
            publicKey ?? keys.PublicKeyPemBase64,
            "test-key-2026-08");

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_rejects_empty_key_id()
    {
        var keys = RsaTestKeys.Create();
        var options = CreateOptions(
            keys.PrivateKeyPemBase64,
            keys.PublicKeyPemBase64,
            "");

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Key_material_imports_both_keys_with_the_configured_key_id()
    {
        var options = ValidOptions(RsaTestKeys.Create());

        using var material = new JwtRsaKeyMaterial(Options.Create(options));

        Assert.Equal(options.KeyId, material.SigningKey.KeyId);
        Assert.Equal(options.KeyId, material.ValidationKey.KeyId);
        Assert.True(material.SigningKey.Rsa.KeySize >= 2048);
        Assert.True(material.ValidationKey.Rsa.KeySize >= 2048);
    }

    private static JwtOptions ValidOptions(RsaTestKeys keys) =>
        CreateOptions(
            keys.PrivateKeyPemBase64,
            keys.PublicKeyPemBase64,
            "test-key-2026-08");

    private static JwtOptions CreateOptions(
        string privateKeyPemBase64,
        string publicKeyPemBase64,
        string keyId) => new()
    {
        Issuer = "Veterinaria.Api.Tests",
        Audience = "Veterinaria.Client.Tests",
        PrivateKeyPemBase64 = privateKeyPemBase64,
        PublicKeyPemBase64 = publicKeyPemBase64,
        KeyId = keyId,
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
        ClockSkewSeconds = 0
    };
}
