using System.Reflection;
using Api.Auth.Controllers;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Users.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// U5 retiró UserCredentialsController (el reset de contraseña ajena exclusivo
// de SuperAdmin, antes SEC-02) junto con USER_ACCOUNTS/USER_CREDENTIALS. La
// misma capacidad quedó en UsersController.ResetPassword, operando directo
// sobre Users.PasswordHash. El autoservicio (propia cuenta) sigue en
// AuthController sin restricción de rol.
public sealed class ChangePasswordAuthorizationTests
{
    [Fact]
    public void UsersController_ResetPassword_requires_SuperAdminOnly_policy()
    {
        var method = typeof(UsersController).GetMethod(
            nameof(UsersController.ResetPassword));
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.SuperAdminOnly, authorizeAttr.Policy);
    }

    [Fact]
    public void UsersController_ResetPassword_no_longer_uses_RequirePermission()
    {
        var method = typeof(UsersController).GetMethod(
            nameof(UsersController.ResetPassword));
        Assert.NotNull(method);

        Assert.Null(method.GetCustomAttribute<RequirePermissionAttribute>());
    }

    [Fact]
    public void AuthController_ChangeMyPassword_only_requires_authentication()
    {
        var method = typeof(AuthController).GetMethod(
            nameof(AuthController.ChangeMyPassword));
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Null(authorizeAttr.Policy);
        Assert.Null(method.GetCustomAttribute<RequirePermissionAttribute>());
    }
}
