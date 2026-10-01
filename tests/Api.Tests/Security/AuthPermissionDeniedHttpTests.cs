using System.Net;
using Xunit;

namespace Api.Tests.Security;

// S15-3: a real, validly-authenticated staff token with no module permission
// (AuthSecurityHttpApiFactory issues zero perm:* claims for every login) must be
// rejected with 403 by a real RequirePermission-protected endpoint, not just in
// the happy-path case. This is distinct from PlatformAccessDeniedAuthContractTests,
// which covers Cliente being denied at login time (no platform access at all) —
// here the actor already has platform access, just not this specific permission.
[Collection("AuthSecurityHttp")]
public sealed class AuthPermissionDeniedHttpTests
{
    private readonly AuthSecurityHttpApiFactory factory;

    public AuthPermissionDeniedHttpTests(AuthSecurityHttpApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task Staff_token_without_Roles_View_permission_receives_403_on_protected_endpoint()
    {
        using var client = factory.CreateAnonymousClient();
        var tokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.StaffAEmail,
            AuthSecurityTestUsers.StaffAPassword);

        AuthSecurityHttpTestHelpers.WithBearer(client, tokens.AccessToken);
        using var response = await client.GetAsync("/api/Roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
