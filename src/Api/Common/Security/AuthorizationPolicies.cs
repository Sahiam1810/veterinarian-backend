namespace Api.Common.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string TelegramAgentOnly = "TelegramAgentOnly";

    // Consulta de historiales clínicos (historias médicas, vacunas): todos
    // los roles con interés legítimo en ver el historial de una mascota,
    // incluido el cliente dueño. La escritura la controla cada policy propia.
    public const string ClinicalHistoryReadOnly = "ClinicalHistoryReadOnly";
}
