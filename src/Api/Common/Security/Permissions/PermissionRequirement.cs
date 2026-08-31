using Microsoft.AspNetCore.Authorization;

namespace Api.Common.Security.Permissions;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string moduleName, PermissionAction action)
    {
        ModuleName = moduleName;
        Action = action;
    }

    public string ModuleName { get; }

    public PermissionAction Action { get; }
}
