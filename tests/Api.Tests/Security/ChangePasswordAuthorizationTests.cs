using System.Reflection;
using Api.Auth.Controllers;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.UserCredentials.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// SEC-02: el reset de contraseña ajena queda exclusivo de SuperAdmin; el
// autoservicio (propia cuenta) vive en AuthController sin restricción de rol.
public sealed class ChangePasswordAuthorizationTests
{
    [Fact]
    public void UserCredentialsController_ChangePassword_requires_SuperAdminOnly_policy()
    {
        var method = typeof(UserCredentialsController).GetMethod(
            nameof(UserCredentialsController.ChangePassword));
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.SuperAdminOnly, authorizeAttr.Policy);
    }

    [Fact]
    public void UserCredentialsController_ChangePassword_no_longer_uses_RequirePermission()
    {
        var method = typeof(UserCredentialsController).GetMethod(
            nameof(UserCredentialsController.ChangePassword));
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
