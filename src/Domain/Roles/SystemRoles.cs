namespace Domain.Roles;

public static class SystemRoles
{
    public static readonly Guid SuperAdminId =
        Guid.Parse("99999999-9999-9999-9999-999999999999");

    public const string SuperAdminName = "SuperAdmin";

    public static bool IsSuperAdmin(Guid roleId) =>
        roleId == SuperAdminId;

    public static bool IsReservedName(string name) =>
        string.Equals(
            name.Trim(),
            SuperAdminName,
            StringComparison.OrdinalIgnoreCase);
}
