namespace Application.Telegram.Errors;

public abstract class TelegramIntegrationException(string message) : Exception(message);

public sealed class TelegramAccountUnavailableException()
    : TelegramIntegrationException("The linked Huellitas account is unavailable.");

public sealed class TelegramLinkCodeInvalidException()
    : TelegramIntegrationException("The Telegram link code is invalid or expired.");

public sealed class TelegramIdentityConflictException()
    : TelegramIntegrationException("The Telegram identity is linked to another account.");
