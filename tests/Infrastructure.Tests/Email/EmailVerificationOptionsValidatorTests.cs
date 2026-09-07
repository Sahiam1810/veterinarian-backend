using Infrastructure.Email.Configuration;
using Xunit;

namespace Infrastructure.Tests.Email;

public sealed class EmailVerificationOptionsValidatorTests
{
    private readonly EmailVerificationOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        var options = new EmailVerificationOptions
        {
            OtpTtlMinutes = 5,
            OtpMaximumAttempts = 5,
            OtpResendSeconds = 60
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithInvalidOptions_ReturnsFailure()
    {
        var options = new EmailVerificationOptions
        {
            OtpTtlMinutes = 0,
            OtpMaximumAttempts = -1,
            OtpResendSeconds = -5
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("OtpTtlMinutes"));
        Assert.Contains(result.Failures, f => f.Contains("OtpMaximumAttempts"));
        Assert.Contains(result.Failures, f => f.Contains("OtpResendSeconds"));
    }
}
