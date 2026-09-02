namespace Application.Telegram.Abstractions;

public interface ITelegramRegistrationProtector
{
    string GenerateCompletionToken();

    string HashCompletionToken(string token);

    string ProtectEmail(string normalizedEmail);

    string UnprotectEmail(string protectedEmail);
}
