using Domain.Verification.Enums;
using Infrastructure.Email;
using Infrastructure.Email.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class SmtpEmailVerificationCodeSenderTests
{
    [Fact]
    public async Task Sender_builds_neutral_verification_email()
    {
        var transport = Substitute.For<ISmtpTransport>();
        var options = Options.Create(ValidOptions());
        var sender = new SmtpEmailVerificationCodeSender(options, transport);
        var expiration = new DateTimeOffset(2026, 8, 31, 20, 5, 0, TimeSpan.Zero);

        Assert.Equal(VerificationDeliveryChannel.Email, sender.Channel);
        await sender.SendAsync("cliente@huellitas.test", "123456", expiration, default);

        await transport.Received(1).SendAsync(
            Arg.Is<SmtpEnvelope>(envelope =>
                envelope.Destination == "cliente@huellitas.test" &&
                envelope.Subject == "Código de verificación de Huellitas" &&
                envelope.Body.Contains("123456", StringComparison.Ordinal) &&
                envelope.Body.Contains("2026-08-31 20:05 UTC", StringComparison.Ordinal)),
            default);
    }

    [Fact]
    public void Enabled_email_options_require_smtp_credentials()
    {
        var result = new EmailOptionsValidator().Validate(
            null,
            new EmailOptions { Enabled = true });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("Host"));
        Assert.Contains(result.Failures!, failure => failure.Contains("Password"));
        Assert.Contains(result.Failures!, failure => failure.Contains("FromAddress"));
    }

    private static EmailOptions ValidOptions() => new()
    {
        Enabled = true,
        Host = "smtp.huellitas.test",
        Port = 587,
        Username = "mailer",
        Password = "secret",
        FromAddress = "no-reply@huellitas.test",
        FromName = "Huellitas",
        UseTls = true
    };
}
