namespace Application.Telegram.Abstractions;

public interface ITelegramRuntimeSettings
{
    bool GuestModeEnabled { get; }

    string BotUsername { get; }

    TimeSpan LinkCodeTtl { get; }

    TimeSpan WorkerPollInterval { get; }

    TimeSpan ProcessingLease { get; }

    int MaxProcessingAttempts { get; }

    TimeSpan DelegatedTokenLifetime { get; }

    // Estado inicial (Pendiente) para un ChatEscalation creado automáticamente
    // al detectar una frase de escalamiento (Ticket B2). Configurable en vez de
    // hardcodeado, mismo criterio que Agent__InitialConversationStatusId.
    Guid PendingEscalationStatusId { get; }

    TimeSpan OtpLifetime { get; }

    int OtpMaximumAttempts { get; }

    TimeSpan OtpResendInterval { get; }

    TimeSpan PrivateAccessAbsoluteLifetime { get; }

    TimeSpan PrivateAccessIdleLifetime { get; }

    bool RegistrationEnabled { get; }

    string RegistrationCompletionUrl { get; }

    TimeSpan RegistrationOtpLifetime { get; }

    TimeSpan RegistrationTokenLifetime { get; }

    int RegistrationMaximumOtpAttempts { get; }

    TimeSpan RegistrationResendInterval { get; }
}
