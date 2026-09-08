using System.Security.Claims;
using Api.Common.Security;
using Api.Extensions;
using Domain.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

// Prueba las policies tal como quedan registradas por AddApiAuthorizationPolicies,
// sin levantar la API completa: solo el pipeline de autorización.
public sealed class AuthorizationPoliciesTests
{
    private readonly IAuthorizationService authorizationService;

    public AuthorizationPoliciesTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ISender>());
        services.AddApiAuthorizationPolicies();

        authorizationService = services.BuildServiceProvider()
            .GetRequiredService<IAuthorizationService>();
    }

    [Theory]
    [InlineData(AuthorizationPolicies.AdminOnly)]
    [InlineData(AuthorizationPolicies.StaffOnly)]
    [InlineData(AuthorizationPolicies.ClinicalHistoryReadOnly)]
    public async Task Role_based_policies_allow_the_persisted_SuperAdmin_role(string policy)
    {
        var superAdmin = PersistedSuperAdmin();

        var result = await authorizationService.AuthorizeAsync(superAdmin, policy);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task StaffOnly_still_denies_a_user_with_no_role()
    {
        var anonymous = PrincipalWithClaims();

        var result = await authorizationService.AuthorizeAsync(anonymous, AuthorizationPolicies.StaffOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task StaffOnly_still_allows_a_regular_role_exactly_as_before()
    {
        var veterinarian = PrincipalWithClaims(new Claim(ClaimTypes.Role, "Veterinario"));

        var result = await authorizationService.AuthorizeAsync(veterinarian, AuthorizationPolicies.StaffOnly);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SuperAdminOnly_allows_the_canonical_role_id()
    {
        var superAdmin = PersistedSuperAdmin();

        var result = await authorizationService.AuthorizeAsync(superAdmin, AuthorizationPolicies.SuperAdminOnly);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SuperAdminOnly_still_rejects_a_regular_role()
    {
        var administrator = PrincipalWithClaims(new Claim(ClaimTypes.Role, "Administrador"));

        var result = await authorizationService.AuthorizeAsync(administrator, AuthorizationPolicies.SuperAdminOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SuperAdminOnly_rejects_the_obsolete_forgeable_claim()
    {
        var legacyClaim = PrincipalWithClaims(new Claim("super_admin", "true"));

        var result = await authorizationService.AuthorizeAsync(
            legacyClaim,
            AuthorizationPolicies.SuperAdminOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task TelegramAgentOnly_allows_a_verified_delegated_identity()
    {
        var delegated = PrincipalWithClaims(new Claim("token_use", "telegram_agent"));

        var result = await authorizationService.AuthorizeAsync(delegated, "TelegramAgentOnly");

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("telegram_guest")]
    [InlineData("TELEGRAM_AGENT")]
    public async Task TelegramAgentOnly_rejects_non_delegated_identities(string? tokenUse)
    {
        var claims = tokenUse is null
            ? Array.Empty<Claim>()
            : new[] { new Claim("token_use", tokenUse) };
        var principal = PrincipalWithClaims(claims);

        var result = await authorizationService.AuthorizeAsync(principal, "TelegramAgentOnly");

        Assert.False(result.Succeeded);
    }

    private static ClaimsPrincipal PersistedSuperAdmin() =>
        PrincipalWithClaims(
            new Claim("role_id", SystemRoles.SuperAdminId.ToString()),
            new Claim(ClaimTypes.Role, SystemRoles.SuperAdminName));

    private static ClaimsPrincipal PrincipalWithClaims(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
}
