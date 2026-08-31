namespace Application.Telegram.Abstractions;

public sealed record GeneratedTelegramOtp(string Code, string Hash);

public interface ITelegramOtpProtector
{
    GeneratedTelegramOtp Create();

    bool Verify(string code, string expectedHash);

    string HashEmail(string normalizedEmail);
}
