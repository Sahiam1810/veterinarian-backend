using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Application.Permissions.Claims;
using Application.Security.Errors;
using Domain.Roles;
using Xunit;

namespace Api.Tests.Security;

// U7 -- tests de aceptación del "Frente 2: fusión de usuarios/cuentas":
// alta + login con sub == id de usuario, ausencia de excepciones de permiso
// por usuario y bloqueo de refresh para un usuario desactivado.
[Collection("AuthSecurityHttp")]
public sealed class AuthAcceptanceHttpTests
{
    private readonly AuthSecurityHttpApiFactory factory;

    public AuthAcceptanceHttpTests(AuthSecurityHttpApiFactory factory) =>
        this.factory = factory;

    // "un usuario nuevo creado con POST /api/users inicia sesión; el sub de
    // su token es su id"
    [Fact]
    public async Task New_user_created_via_post_users_logs_in_with_sub_equal_to_its_id()
    {
        using var client = factory.CreateAnonymousClient();
        var superAdminTokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.SuperAdminEmail,
            AuthSecurityTestUsers.SuperAdminPassword);
        AuthSecurityHttpTestHelpers.WithBearer(client, superAdminTokens.AccessToken);

        const string newUserEmail = "nuevo-usuario@huellitas.test";
        const string newUserPassword = "NuevoUsuario123!";
        using var createResponse = await client.PostAsJsonAsync(
            "/api/users",
            new
            {
                FullName = "Usuario Nuevo",
                Email = newUserEmail,
                Password = newUserPassword,
                RoleId = factory.StaffRoleId
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateUserResponseDto>();
        Assert.NotNull(created);

        client.DefaultRequestHeaders.Authorization = null;
        var loginTokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            newUserEmail,
            newUserPassword);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(loginTokens.AccessToken);
        var sub = token.Claims.Single(claim => claim.Type == "sub").Value;
        Assert.Equal(created!.Id.ToString(), sub);
    }

    // "una excepción de permiso por usuario ya no existe": dos usuarios con
    // el mismo rol reciben exactamente los mismos claims de permiso -- no
    // hay ningún mecanismo que agregue/quite permisos por usuario individual.
    [Fact]
    public async Task Two_users_with_the_same_role_get_identical_permission_claims()
    {
        using var client = factory.CreateAnonymousClient();
        var tokensA = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.StaffAEmail,
            AuthSecurityTestUsers.StaffAPassword);
        var tokensB = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.StaffBEmail,
            AuthSecurityTestUsers.StaffBPassword);

        var handler = new JwtSecurityTokenHandler();
        var permissionsA = handler.ReadJwtToken(tokensA.AccessToken).Claims
            .Where(claim => claim.Type == PermissionClaimValue.ClaimType)
            .Select(claim => claim.Value)
            .ToArray();
        var permissionsB = handler.ReadJwtToken(tokensB.AccessToken).Claims
            .Where(claim => claim.Type == PermissionClaimValue.ClaimType)
            .Select(claim => claim.Value)
            .ToArray();

        Assert.Equal(permissionsA, permissionsB);
    }

    // "un usuario desactivado no puede renovar sesión". Usa un usuario
    // dedicado (no staffA/staffB) para no desactivar de forma permanente un
    // usuario que otros tests de la misma fixture compartida siguen usando.
    [Fact]
    public async Task Deactivated_user_cannot_refresh_session()
    {
        using var client = factory.CreateAnonymousClient();
        var superAdminTokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.SuperAdminEmail,
            AuthSecurityTestUsers.SuperAdminPassword);
        AuthSecurityHttpTestHelpers.WithBearer(client, superAdminTokens.AccessToken);

        const string throwawayEmail = "para-desactivar@huellitas.test";
        const string throwawayPassword = "ParaDesactivar123!";
        using var createResponse = await client.PostAsJsonAsync(
            "/api/users",
            new
            {
                FullName = "Usuario Para Desactivar",
                Email = throwawayEmail,
                Password = throwawayPassword,
                RoleId = factory.StaffRoleId
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var tokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            throwawayEmail,
            throwawayPassword);

        factory.DeactivateUser(throwawayEmail);

        using var refreshResponse = await AuthSecurityHttpTestHelpers.RefreshAsync(
            client,
            tokens.RefreshToken);

        var problem = await AuthSecurityHttpTestHelpers.ReadAuthProblemAsync(refreshResponse);
        Assert.Equal(HttpStatusCode.Forbidden, problem.StatusCode);
        Assert.Equal(AuthenticationErrors.UserInactive.Code, problem.Code);
    }

    private sealed record CreateUserResponseDto(Guid Id);
}
