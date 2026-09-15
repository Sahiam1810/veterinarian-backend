using System.Reflection;
using Api.Common.Security.Permissions;
using Api.Vaccinations.Controllers;
using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Vaccinations;

public sealed class VaccinationsAuthorizationTests
{
    [Theory]
    [InlineData(nameof(VaccinationsController.GetAll))]
    [InlineData(nameof(VaccinationsController.GetById))]
    public void General_reads_require_only_HistorialesClinicos_View_permission(string methodName)
    {
        var method = typeof(VaccinationsController).GetMethod(methodName);

        Assert.NotNull(method);
        var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>().ToArray();
        var authorizeAttribute = Assert.Single(authorizeAttributes);
        Assert.IsType<RequirePermissionAttribute>(authorizeAttribute);
        Assert.Equal($"perm:Historiales Clínicos:{PermissionAction.View}", authorizeAttribute.Policy);
    }

    [Fact]
    public async Task Custom_role_with_HistorialesClinicos_View_permission_succeeds_without_being_ClinicalStaff()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Historiales Clínicos", PermissionAction.View);
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "AuditorClinicoExterno"),
                new System.Security.Claims.Claim(PermissionClaimValue.ClaimType, "perm:Historiales Clínicos:View")
            ],
            authenticationType: "TestAuth");

        var context = new AuthorizationHandlerContext([requirement], new System.Security.Claims.ClaimsPrincipal(identity), null);
        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Custom_role_without_HistorialesClinicos_View_permission_fails()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Historiales Clínicos", PermissionAction.View);
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "RolSinClinica")],
            authenticationType: "TestAuth");

        var context = new AuthorizationHandlerContext([requirement], new System.Security.Claims.ClaimsPrincipal(identity), null);
        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}