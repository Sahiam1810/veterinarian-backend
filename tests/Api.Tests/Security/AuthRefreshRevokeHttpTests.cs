using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Api.Tests.Security;

// S15-4 / S15-5: refresh + revoke over real HTTP with real AuthenticationService.
[Collection("AuthSecurityHttp")]
public sealed class AuthRefreshRevokeHttpTests
{
    private readonly AuthSecurityHttpApiFactory factory;

    public AuthRefreshRevokeHttpTests(AuthSecurityHttpApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public async Task Refresh_after_revoke_returns_401_InvalidRefreshToken()
    {
        using var client = factory.CreateAnonymousClient();
        var tokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.StaffAEmail,
            AuthSecurityTestUsers.StaffAPassword);

        AuthSecurityHttpTestHelpers.WithBearer(client, tokens.AccessToken);
        using var revokeResponse = await AuthSecurityHttpTestHelpers.RevokeAsync(
            client,
            tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        using var refreshResponse = await AuthSecurityHttpTestHelpers.RefreshAsync(
            client,
            tokens.RefreshToken);

        var problem = await AuthSecurityHttpTestHelpers.ReadAuthProblemAsync(refreshResponse);
        AuthSecurityHttpTestHelpers.AssertInvalidRefreshTokenProblem(problem);
        AuthSecurityHttpTestHelpers.AssertNoPii(
            problem,
            AuthSecurityTestUsers.StaffAEmail,
            tokens.RefreshToken,
            tokens.AccessToken);
    }

    [Fact]
    public async Task Revoke_foreign_and_nonexistent_refresh_tokens_return_indistinguishable_401_responses()
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

        AuthSecurityHttpTestHelpers.WithBearer(client, tokensB.AccessToken);
        using var foreignResponse = await AuthSecurityHttpTestHelpers.RevokeAsync(
            client,
            tokensA.RefreshToken);
        var foreignProblem = await AuthSecurityHttpTestHelpers.ReadAuthProblemAsync(foreignResponse);

        using var nonexistentResponse = await AuthSecurityHttpTestHelpers.RevokeAsync(
            client,
            "nonexistent-refresh-token-value");
        var nonexistentProblem = await AuthSecurityHttpTestHelpers.ReadAuthProblemAsync(nonexistentResponse);

        AuthSecurityHttpTestHelpers.AssertInvalidRefreshTokenProblem(foreignProblem);
        AuthSecurityHttpTestHelpers.AssertInvalidRefreshTokenProblem(nonexistentProblem);
        AuthSecurityHttpTestHelpers.AssertAntiEnumerationResponsesMatch(
            foreignProblem,
            nonexistentProblem);
        AuthSecurityHttpTestHelpers.AssertNoPii(
            foreignProblem,
            AuthSecurityTestUsers.StaffAEmail,
            AuthSecurityTestUsers.StaffBEmail,
            tokensA.RefreshToken,
            tokensB.RefreshToken);
        AuthSecurityHttpTestHelpers.AssertNoPii(
            nonexistentProblem,
            AuthSecurityTestUsers.StaffAEmail,
            AuthSecurityTestUsers.StaffBEmail,
            tokensA.RefreshToken,
            tokensB.RefreshToken);
    }
}

[Collection("AuthSecurityHttp")]
public sealed class AuthClienteNoPasswordHashHttpTests
{
    private readonly AuthSecurityHttpApiFactory factory;

    public AuthClienteNoPasswordHashHttpTests(AuthSecurityHttpApiFactory factory) =>
        this.factory = factory;

    [Theory]
    [InlineData(AuthSecurityTestUsers.ClientePlausiblePassword)]
    [InlineData(AuthSecurityTestUsers.ClienteRandomPassword)]
    public async Task Login_cliente_without_password_hash_returns_401_InvalidCredentials(string password)
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = AuthSecurityTestUsers.ClienteNoHashEmail,
                Password = password
            });

        var problem = await AuthSecurityHttpTestHelpers.ReadAuthProblemAsync(response);
        AuthSecurityHttpTestHelpers.AssertInvalidCredentialsProblem(problem);
        AuthSecurityHttpTestHelpers.AssertNoPii(
            problem,
            AuthSecurityTestUsers.ClienteNoHashEmail,
            password,
            "Cliente");
    }
}

[Collection("AuthSecurityHttp")]
public sealed class AuthPostRevokeAccessTokenTests
{
    private readonly AuthSecurityHttpApiFactory factory;

    public AuthPostRevokeAccessTokenTests(AuthSecurityHttpApiFactory factory) =>
        this.factory = factory;

    // Expected JWT stateless behavior: revoking refresh does not invalidate access until exp.
    [Fact]
    public async Task Me_still_returns_200_with_same_access_token_after_refresh_revoke()
    {
        using var client = factory.CreateAnonymousClient();
        var tokens = await AuthSecurityHttpTestHelpers.LoginAsync(
            client,
            AuthSecurityTestUsers.StaffAEmail,
            AuthSecurityTestUsers.StaffAPassword);

        AuthSecurityHttpTestHelpers.WithBearer(client, tokens.AccessToken);
        using var beforeRevoke = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, beforeRevoke.StatusCode);

        using (var revokeResponse = await AuthSecurityHttpTestHelpers.RevokeAsync(
                   client,
                   tokens.RefreshToken))
        {
            Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        }

        using var afterRevoke = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, afterRevoke.StatusCode);

        using var document = JsonDocument.Parse(await afterRevoke.Content.ReadAsStringAsync());
        Assert.Equal(
            AuthSecurityTestUsers.StaffAEmail,
            document.RootElement.GetProperty("email").GetString());
    }
}
