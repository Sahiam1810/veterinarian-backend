using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Email;
using Infrastructure.Email.Configuration;
using Infrastructure.Telegram.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Tests.Telegram;

public sealed class SmtpTelegramVerificationCodeSenderTests
{
    [Fact]
    public async Task Sender_builds_neutral_verification_email()
    {
        var transport = Substitute.For<ISmtpTransport>();
        var options = Options.Create(ValidOptions());
        var sender = new SmtpTelegramVerificationCodeSender(options, transport);
        var expiration = new DateTimeOffset(2026, 8, 31, 20, 5, 0, TimeSpan.Zero);

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

    [Fact]
    public async Task Account_lookup_returns_only_an_active_identity()
    {
        var accounts = Substitute.For<IUserAccountsRepository>();
        var users = Substitute.For<IUsersRepository>();
        var user = new UserEntity(
            "Cliente Huellitas",
            "cliente@huellitas.test",
            "unused-hash",
            Guid.NewGuid());
        var account = new UserAccountEntity(
            user.Id,
            "cliente",
            "cliente@huellitas.test",
            "Activo");
        accounts.GetByMailAsync("cliente@huellitas.test", default).Returns(account);
        users.GetByIdAsync(user.Id, default).Returns(user);
        var lookup = new TelegramAccountLookup(accounts, users);

        var result = await lookup.FindActiveByEmailAsync(
            " Cliente@Huellitas.Test ",
            default);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.PersonId);
        Assert.Equal("cliente@huellitas.test", result.Email);
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
