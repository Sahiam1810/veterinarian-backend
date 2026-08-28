using System.Net.Http.Headers;
using Application.Agent.Abstractions;
using Application.Common.Exceptions;

namespace Api.Agent.Security;

public sealed class HttpContextUserAccessTokenProvider(
    IHttpContextAccessor httpContextAccessor) : IUserAccessTokenProvider
{
    public string GetRequiredAccessToken()
    {
        var headers = httpContextAccessor.HttpContext?.Request.Headers.Authorization;
        var raw = headers?.Count == 1 ? headers.Value.ToString() : null;
        if (!AuthenticationHeaderValue.TryParse(raw, out var authorization) ||
            !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            throw new UnauthorizedException("Authenticated access token is unavailable.");
        }

        return authorization.Parameter;
    }
}
