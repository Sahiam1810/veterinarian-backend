using Application.Verification.Abstractions;
using Infrastructure.Verification.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verification;

public sealed class ConfiguredAppointmentVerificationSettings(
    IOptions<AppointmentVerificationOptions> options) : IAppointmentVerificationSettings
{
    private readonly AppointmentVerificationOptions _options = options.Value;

    public TimeSpan OtpLifetime => TimeSpan.FromMinutes(_options.OtpTtlMinutes);

    public int OtpMaximumAttempts => _options.OtpMaximumAttempts;

    public TimeSpan OtpResendInterval => TimeSpan.FromSeconds(_options.OtpResendSeconds);
}
