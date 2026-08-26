namespace Api.Common.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string VeterinarianOnly = "VeterinarianOnly";
    public const string ReceptionistOnly = "ReceptionistOnly";
    public const string AssistantOnly = "AssistantOnly";
    public const string ClientOnly = "ClientOnly";
    public const string StaffOnly = "StaffOnly";
}