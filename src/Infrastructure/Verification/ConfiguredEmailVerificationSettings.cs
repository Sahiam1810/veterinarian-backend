using Application.Verification.Abstractions;
using Infrastructure.Email.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verification;

public sealed class ConfiguredEmailVerificationSettings(
    IOptions<EmailVerificationOptions> options) : IEmailVerificationSettings
{
    private readonly EmailVerificationOptions _options = options.Value;

    public TimeSpan OtpLifetime => TimeSpan.FromMinutes(_options.OtpTtlMinutes);

    public int OtpMaximumAttempts => _options.OtpMaximumAttempts;

    public TimeSpan OtpResendInterval => TimeSpan.FromSeconds(_options.OtpResendSeconds);
}
