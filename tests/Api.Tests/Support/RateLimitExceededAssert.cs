using System.Net;
using System.Text.Json;
using Xunit;

namespace Api.Tests.Support;

// Contrato central OnRejected: 429 + problem+json + RateLimit.Exceeded.
internal static class RateLimitExceededAssert
{
    public static async Task EqualsContractAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(429, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            "RateLimit.Exceeded",
            document.RootElement.GetProperty("code").GetString());
    }
}
