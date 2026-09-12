namespace Application.Telegram.Abstractions;

public interface ITelegramIdentityDataProtector
{
    string Protect(string purpose, string value);

    string Unprotect(string purpose, string protectedValue);
}
