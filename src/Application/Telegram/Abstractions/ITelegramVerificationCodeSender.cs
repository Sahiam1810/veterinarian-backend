namespace Application.Telegram.Abstractions;

public interface ITelegramVerificationCodeSender
{
    Task SendAsync(
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
