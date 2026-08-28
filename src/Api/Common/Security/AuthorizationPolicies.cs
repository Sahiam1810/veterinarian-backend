namespace Api.Common.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string VeterinarianOnly = "VeterinarianOnly";
    public const string ReceptionistOnly = "ReceptionistOnly";
    public const string AssistantOnly = "AssistantOnly";
    public const string ClientOnly = "ClientOnly";
    public const string StaffOnly = "StaffOnly";

    // Políticas combinadas para acciones que corresponden a más de un rol.
    public const string AdminOrReceptionist = "AdminOrReceptionist";
    public const string AdminOrVeterinarian = "AdminOrVeterinarian";
    public const string ClinicalStaffOnly = "ClinicalStaffOnly";
    public const string FrontDeskStaffOnly = "FrontDeskStaffOnly";

    // Consulta de historiales clínicos (historias médicas, vacunas): todos
    // los roles con interés legítimo en ver el historial de una mascota,
    // incluido el cliente dueño. La escritura la controla cada policy propia.
    public const string ClinicalHistoryReadOnly = "ClinicalHistoryReadOnly";
}