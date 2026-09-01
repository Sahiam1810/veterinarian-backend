using System.Reflection;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.UserTokens.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// SEC-04: manipular tokens de sesión de cualquier cuenta (crearlos a mano,
// verlos, borrarlos) quedó exclusivo de SuperAdmin -- mismo criterio que
// SEC-02 (reset de contraseña ajena). Antes era RequirePermission("Usuarios",
// ...), que dejaba a Administrador (sin ser SuperAdmin) manipular tokens.
public sealed class UserTokensAuthorizationTests
{
    [Theory]
    [InlineData(nameof(UserTokensController.Create))]
    [InlineData(nameof(UserTokensController.GetById))]
    [InlineData(nameof(UserTokensController.GetByAccountId))]
    [InlineData(nameof(UserTokensController.Delete))]
    public void UserTokensController_actions_require_SuperAdminOnly_policy(string methodName)
    {
        var method = typeof(UserTokensController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.SuperAdminOnly, authorizeAttr.Policy);
        Assert.Null(method.GetCustomAttribute<RequirePermissionAttribute>());
    }
}
