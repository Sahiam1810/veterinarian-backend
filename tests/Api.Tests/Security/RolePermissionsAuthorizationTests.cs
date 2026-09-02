using System.Reflection;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.RolePermissions.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// La gestión de permisos por rol es exclusiva de SuperAdmin -- no pasa por
// RequirePermission ni se puede delegar vía UserPermission (sería circular).
public sealed class RolePermissionsAuthorizationTests
{
    [Theory]
    [InlineData(nameof(RolePermissionsController.GetAll))]
    [InlineData(nameof(RolePermissionsController.GetById))]
    [InlineData(nameof(RolePermissionsController.GetByRoleId))]
    [InlineData(nameof(RolePermissionsController.Create))]
    [InlineData(nameof(RolePermissionsController.Update))]
    [InlineData(nameof(RolePermissionsController.Delete))]
    public void RolePermissionsController_actions_require_SuperAdminOnly_policy(string methodName)
    {
        var method = typeof(RolePermissionsController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.SuperAdminOnly, authorizeAttr.Policy);
        Assert.Null(method.GetCustomAttribute<RequirePermissionAttribute>());
    }
}
