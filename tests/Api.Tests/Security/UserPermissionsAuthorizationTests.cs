using System.Reflection;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.UserPermissions.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// La gestión de permisos puntuales por usuario es exclusiva de SuperAdmin --
// no pasa por RequirePermission, mismo criterio que RolePermissions.
public sealed class UserPermissionsAuthorizationTests
{
    [Theory]
    [InlineData(nameof(UserPermissionsController.GetAll))]
    [InlineData(nameof(UserPermissionsController.GetById))]
    [InlineData(nameof(UserPermissionsController.GetByUserId))]
    [InlineData(nameof(UserPermissionsController.Create))]
    [InlineData(nameof(UserPermissionsController.Update))]
    [InlineData(nameof(UserPermissionsController.Delete))]
    public void UserPermissionsController_actions_require_SuperAdminOnly_policy(string methodName)
    {
        var method = typeof(UserPermissionsController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.SuperAdminOnly, authorizeAttr.Policy);
        Assert.Null(method.GetCustomAttribute<RequirePermissionAttribute>());
    }
}
