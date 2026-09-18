using Application.Common.Results;

namespace Application.Security.Errors;

// Codes estables de rate limit (429). El front/bot traduce por code, no por title.
public static class RateLimitErrors
{
    private const string GenericDescription = "Rate limit exceeded.";

    // 429: demasiadas solicitudes en la ventana de la policy.
    public static readonly Error Exceeded = new(
        "RateLimit.Exceeded",
        GenericDescription);
}
