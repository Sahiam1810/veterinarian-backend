using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Security.Errors;
using Xunit;

namespace Api.Tests.Security;

internal static class AuthSecurityHttpTestHelpers
{
    internal sealed record AuthTokens(string AccessToken, string RefreshToken);

    internal sealed record AuthProblemSnapshot(
        HttpStatusCode StatusCode,
        string? ContentType,
        string? Type,
        string? Title,
        int? Status,
        string? Code,
        string RawPayload);

    public static async Task<AuthTokens> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = password });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return new AuthTokens(
            document.RootElement.GetProperty("accessToken").GetString()!,
            document.RootElement.GetProperty("refreshToken").GetString()!);
    }

    public static HttpClient WithBearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task<HttpResponseMessage> RevokeAsync(
        HttpClient client,
        string refreshToken)
    {
        return await client.PostAsJsonAsync(
            "/api/auth/revoke",
            new { RefreshToken = refreshToken });
    }

    public static async Task<HttpResponseMessage> RefreshAsync(
        HttpClient client,
        string refreshToken)
    {
        return await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { RefreshToken = refreshToken });
    }

    public static async Task<AuthProblemSnapshot> ReadAuthProblemAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        string? type = null;
        string? title = null;
        int? status = null;
        string? code = null;

        if (!string.IsNullOrWhiteSpace(payload))
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.TryGetProperty("type", out var typeElement))
            {
                type = typeElement.GetString();
            }

            if (root.TryGetProperty("title", out var titleElement))
            {
                title = titleElement.GetString();
            }

            if (root.TryGetProperty("status", out var statusElement))
            {
                status = statusElement.GetInt32();
            }

            if (root.TryGetProperty("code", out var codeElement))
            {
                code = codeElement.GetString();
            }
        }

        return new AuthProblemSnapshot(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            type,
            title,
            status,
            code,
            payload);
    }

    public static void AssertInvalidRefreshTokenProblem(AuthProblemSnapshot problem)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, problem.StatusCode);
        Assert.Equal("application/problem+json", problem.ContentType);
        Assert.Equal(AuthenticationErrors.InvalidRefreshToken.Code, problem.Code);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.False(problem.RawPayload.Contains("detail", StringComparison.OrdinalIgnoreCase));
    }

    public static void AssertInvalidCredentialsProblem(AuthProblemSnapshot problem)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, problem.StatusCode);
        Assert.Equal("application/problem+json", problem.ContentType);
        Assert.Equal(AuthenticationErrors.InvalidCredentials.Code, problem.Code);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Unauthorized", problem.Title);
    }

    public static void AssertNoPii(AuthProblemSnapshot problem, params string[] forbiddenFragments)
    {
        foreach (var fragment in forbiddenFragments)
        {
            Assert.DoesNotContain(fragment, problem.RawPayload, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("detail", problem.RawPayload, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertAntiEnumerationResponsesMatch(
        AuthProblemSnapshot foreign,
        AuthProblemSnapshot nonexistent)
    {
        Assert.Equal(foreign.StatusCode, nonexistent.StatusCode);
        Assert.Equal(foreign.ContentType, nonexistent.ContentType);
        Assert.Equal(foreign.Type, nonexistent.Type);
        Assert.Equal(foreign.Title, nonexistent.Title);
        Assert.Equal(foreign.Status, nonexistent.Status);
        Assert.Equal(foreign.Code, nonexistent.Code);
    }
}
