using System.Security.Cryptography;
using Infrastructure.Telegram.Security;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramRegistrationProtectorTests
{
    private readonly TelegramRegistrationProtector sut = new(
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    [Fact]
    public void Protected_email_round_trips_without_exposing_plaintext()
    {
        const string email = "ana@huellitas.test";

        var protectedEmail = sut.ProtectEmail(email);

        Assert.DoesNotContain(email, protectedEmail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(email, sut.UnprotectEmail(protectedEmail));
    }

    [Fact]
    public void Generated_completion_tokens_are_random_and_hash_to_sha256_hex()
    {
        var first = sut.GenerateCompletionToken();
        var second = sut.GenerateCompletionToken();

        Assert.NotEqual(first, second);
        Assert.Equal(64, sut.HashCompletionToken(first).Length);
    }

    [Fact]
    public void Modified_ciphertext_is_rejected()
    {
        var protectedEmail = sut.ProtectEmail("ana@huellitas.test");
        var bytes = Convert.FromBase64String(protectedEmail);
        bytes[^1] ^= 1;

        Assert.Throws<AuthenticationTagMismatchException>(() =>
            sut.UnprotectEmail(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Protected_identity_value_round_trips_with_its_purpose()
    {
        var protectedValue = sut.Protect("identification", "123456789");

        Assert.DoesNotContain("123456789", protectedValue, StringComparison.Ordinal);
        Assert.Equal("123456789", sut.Unprotect("identification", protectedValue));
    }

    [Fact]
    public void Protected_identity_value_cannot_be_opened_with_another_purpose()
    {
        var protectedValue = sut.Protect("identification", "123456789");

        Assert.Throws<AuthenticationTagMismatchException>(() =>
            sut.Unprotect("email", protectedValue));
    }
}
