using Infrastructure.Telegram.Security;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramOtpProtectorTests
{
    private static readonly string PepperBase64 = Convert.ToBase64String(
        Enumerable.Range(1, 32).Select(Convert.ToByte).ToArray());

    [Fact]
    public void Protector_verifies_only_the_original_six_digit_code()
    {
        var protector = new TelegramOtpProtector(PepperBase64);

        var generated = protector.Create();
        var differentCode = generated.Code[0] == '0'
            ? $"1{generated.Code[1..]}"
            : $"0{generated.Code[1..]}";

        Assert.Matches("^[0-9]{6}$", generated.Code);
        Assert.True(protector.Verify(generated.Code, generated.Hash));
        Assert.False(protector.Verify(differentCode, generated.Hash));
        Assert.Equal(64, generated.Hash.Length);
        Assert.DoesNotContain(generated.Code, generated.Hash);
    }

    [Fact]
    public void Protector_creates_a_stable_email_hash_without_preserving_email()
    {
        var protector = new TelegramOtpProtector(PepperBase64);

        var first = protector.HashEmail("cliente@huellitas.test");
        var second = protector.HashEmail("cliente@huellitas.test");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.DoesNotContain("cliente", first, StringComparison.OrdinalIgnoreCase);
    }
}
