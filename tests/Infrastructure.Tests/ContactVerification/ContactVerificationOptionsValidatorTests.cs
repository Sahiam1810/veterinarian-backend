using Infrastructure.ContactVerification.Configuration;
using Xunit;

namespace Infrastructure.Tests.ContactVerification;

// Valida bind de ContactVerification (TTL/intentos/resend/proof) distinto a citas.
public sealed class ContactVerificationOptionsValidatorTests
{
    private readonly ContactVerificationOptionsValidator _sut = new();

    [Fact]
    public void Validate_accepts_positive_contact_options()
    {
        var result = _sut.Validate(
            null,
            new ContactVerificationOptions
            {
                OtpTtlMinutes = 10,
                OtpMaximumAttempts = 5,
                OtpResendSeconds = 60,
                ProofTtlMinutes = 15
            });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0, 5, 60, 15)]
    [InlineData(10, 0, 60, 15)]
    [InlineData(10, 5, 0, 15)]
    [InlineData(10, 5, 60, 0)]
    public void Validate_rejects_non_positive_values(
        int ttl,
        int attempts,
        int resend,
        int proofTtl)
    {
        var result = _sut.Validate(
            null,
            new ContactVerificationOptions
            {
                OtpTtlMinutes = ttl,
                OtpMaximumAttempts = attempts,
                OtpResendSeconds = resend,
                ProofTtlMinutes = proofTtl
            });

        Assert.True(result.Failed);
    }
}
