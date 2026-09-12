using Application.ContactVerification.Errors;
using Xunit;

namespace Application.Tests.ContactVerification;

public sealed class ContactVerificationErrorsTests
{
    [Fact]
    public void Catalog_uses_stable_contact_verification_prefix()
    {
        var codes = new[]
        {
            ContactVerificationErrors.InvalidCode.Code,
            ContactVerificationErrors.Expired.Code,
            ContactVerificationErrors.Blocked.Code,
            ContactVerificationErrors.ResendTooSoon.Code,
            ContactVerificationErrors.SessionNotFound.Code,
            ContactVerificationErrors.ProofInvalid.Code,
            ContactVerificationErrors.ProofAlreadyConsumed.Code,
            ContactVerificationErrors.ProofExpired.Code,
            ContactVerificationErrors.ChannelNotSupported.Code,
            ContactVerificationErrors.PurposeInvalid.Code,
            ContactVerificationErrors.EmailInvalid.Code,
            ContactVerificationErrors.DeliveryFailed.Code,
            ContactVerificationErrors.NotImplemented.Code
        };

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.StartsWith("ContactVerification.", code, StringComparison.Ordinal));
    }
}
