namespace Application.Security.Claims;

public static class DelegatedTokenClaims
{
    public const string ClaimType = "token_use";
    public const string TelegramAgent = "telegram_agent";

    // Presente únicamente en tokens de invitado de Telegram (AgentDelegatedIdentityProvider.GetGuest).
    // Permite al nuevo endpoint de vinculación (bot-link) conocer el telegramUserId real
    // sin que el chatbot tenga que reenviarlo, y sin exponerlo a ningún otro flujo.
    public const string TelegramUserId = "telegram_user_id";
}
