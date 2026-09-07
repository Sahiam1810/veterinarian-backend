using Application.ContactVerification.Abstractions;
using Infrastructure.ContactVerification.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.ContactVerification;

public sealed class ConfiguredContactVerificationSettings(
    IOptions<ContactVerificationOptions> options) : IContactVerificationSettings
{
    private readonly ContactVerificationOptions _options = options.Value;

    public TimeSpan OtpLifetime => TimeSpan.FromMinutes(_options.OtpTtlMinutes);

    public int OtpMaximumAttempts => _options.OtpMaximumAttempts;

    public TimeSpan OtpResendInterval => TimeSpan.FromSeconds(_options.OtpResendSeconds);

    public TimeSpan ProofLifetime => TimeSpan.FromMinutes(_options.ProofTtlMinutes);
}
