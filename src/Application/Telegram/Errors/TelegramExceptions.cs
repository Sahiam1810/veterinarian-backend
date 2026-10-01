namespace Application.Telegram.Errors;

using Application.Common.Exceptions;
using Application.Common.Results;

public abstract class TelegramIntegrationException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

public sealed class TelegramAccountUnavailableException()
    : TelegramIntegrationException("The linked Huellitas account is unavailable.");

public sealed class TelegramIdentityConflictException()
    : TelegramIntegrationException("The Telegram identity is linked to another account.");

public sealed class TelegramClientLinkRequiresProofException()
    : ForbiddenException(new Error(
        "Telegram.ClientLinkRequiresProof",
        "This client must be linked with contact verification."));

public sealed class TelegramDeliveryException(Exception? innerException = null)
    : TelegramIntegrationException("Telegram delivery failed.", innerException);
