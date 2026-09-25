using System.Security.Claims;
using Api.Extensions;
using Application.Permissions.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

// Ticket B1: valida el recorrido completo perm:{módulo}:{acción} para los tres
// módulos nuevos — no solo que el atributo esté puesto (ChatPermissionsAuthorizationTests),
// sino que un principal con el claim real de Recepcionista efectivamente pasa,
// y uno sin él efectivamente falla. Mismo arnés que AuthorizationPoliciesTests.
public sealed class ChatPermissionsPolicyPipelineTests
{
    private readonly IAuthorizationService authorizationService;

    public ChatPermissionsPolicyPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ISender>());
        services.AddApiAuthorizationPolicies();

        authorizationService = services.BuildServiceProvider()
            .GetRequiredService<IAuthorizationService>();
    }

    [Theory]
    [InlineData("Chat", "View")]
    [InlineData("Chat", "Create")]
    [InlineData("Chat", "Edit")]
    [InlineData("Escalamientos", "View")]
    [InlineData("Escalamientos", "Create")]
    [InlineData("Escalamientos", "Edit")]
    [InlineData("Catálogos del Chat", "View")]
    public async Task Recepcionista_shaped_principal_with_the_matching_claim_is_authorized(
        string module,
        string action)
    {
        var principal = PrincipalWithPermissionClaims(PermissionClaimValue.Create(module, action));

        var result = await authorizationService.AuthorizeAsync(
            principal,
            $"{RequirePermissionPolicyPrefix}{module}:{action}");

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Chat", "View")]
    [InlineData("Escalamientos", "View")]
    [InlineData("Catálogos del Chat", "View")]
    public async Task Principal_without_the_claim_is_rejected(string module, string action)
    {
        var principal = PrincipalWithPermissionClaims();

        var result = await authorizationService.AuthorizeAsync(
            principal,
            $"{RequirePermissionPolicyPrefix}{module}:{action}");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Chat_View_claim_does_not_grant_Escalamientos_Edit()
    {
        // Los tres módulos son independientes: tener View en Chat no debe colar
        // acceso de Edit en Escalamientos ni en ningún otro módulo/acción.
        var principal = PrincipalWithPermissionClaims(PermissionClaimValue.Create("Chat", "View"));

        var result = await authorizationService.AuthorizeAsync(
            principal,
            $"{RequirePermissionPolicyPrefix}Escalamientos:Edit");

        Assert.False(result.Succeeded);
    }

    private const string RequirePermissionPolicyPrefix = PermissionClaimValue.PolicyPrefix;

    private static ClaimsPrincipal PrincipalWithPermissionClaims(params string[] grantedPermissions)
    {
        var claims = grantedPermissions
            .Select(value => new Claim(PermissionClaimValue.ClaimType, value))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }
}
