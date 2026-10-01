namespace Api.Common.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string TelegramAgentOnly = "TelegramAgentOnly";

    // Solo tokens de invitado de Telegram (portan el claim telegram_user_id).
    // Usado por el endpoint que vincula la cuenta recién registrada/encontrada
    // por el bot, para que nadie autenticado por otra vía pueda invocarlo.
    public const string TelegramGuestLinkOnly = "TelegramGuestLinkOnly";

    // Consulta de historiales clínicos (historias médicas, vacunas): todos
    // los roles con interés legítimo en ver el historial de una mascota,
    // incluido el cliente dueño. La escritura la controla cada policy propia.
}
