namespace Application.Telegram.Errors;

public abstract class TelegramIntegrationException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

public sealed class TelegramAccountUnavailableException()
    : TelegramIntegrationException("The linked Huellitas account is unavailable.");

public sealed class TelegramLinkCodeInvalidException()
    : TelegramIntegrationException("The Telegram link code is invalid or expired.");

public sealed class TelegramIdentityConflictException()
    : TelegramIntegrationException("The Telegram identity is linked to another account.");

public sealed class TelegramDeliveryException(Exception? innerException = null)
    : TelegramIntegrationException("Telegram delivery failed.", innerException);
