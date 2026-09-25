namespace Application.Telegram.Abstractions;

public interface ITelegramRuntimeSettings
{
    bool GuestModeEnabled { get; }

    string BotUsername { get; }

    TimeSpan WorkerPollInterval { get; }

    // Slots paralelos del worker de Telegram (chats distintos). Mismo chat sigue en serie.
    int WorkerConcurrency { get; }

    TimeSpan ProcessingLease { get; }

    int MaxProcessingAttempts { get; }

    TimeSpan DelegatedTokenLifetime { get; }

    // Estado inicial (Pendiente) para un ChatEscalation creado automáticamente
    // al detectar una frase de escalamiento (Ticket B2). Configurable en vez de
    // hardcodeado, mismo criterio que Agent__InitialConversationStatusId.
    Guid PendingEscalationStatusId { get; }

    // SENDER_TYPES "Agente humano" — decide si un ChatMessage se reenvía a
    // Telegram (Ticket B4). Ningún otro remitente (Cliente, Agente IA) dispara
    // el reenvío.
    Guid HumanAgentSenderTypeId { get; }

    TimeSpan PrivateAccessAbsoluteLifetime { get; }

    TimeSpan PrivateAccessIdleLifetime { get; }

    TimeSpan RegistrationLinkWindow { get; }
}
