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

    // Tipo "Texto" de MESSAGE_TYPES, usado al persistir el mensaje del cliente
    // en CHAT_MESSAGES (Ticket B3). El tipo de participante "Cliente" no se
    // repite aquí: se reutiliza IAgentConversationDefaults.ClientParticipantTypeId,
    // la misma fuente que ya usa PersistentConversationContextProvider.
    Guid TextMessageTypeId { get; }

    // SENDER_TYPES "Agente humano" — decide si un ChatMessage se reenvía a
    // Telegram (Ticket B4). Ningún otro remitente (Cliente, Agente IA) dispara
    // el reenvío.
    Guid HumanAgentSenderTypeId { get; }

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
