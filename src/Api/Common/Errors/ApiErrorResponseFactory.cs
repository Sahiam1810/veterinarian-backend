using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Common.Errors;

public static class ApiErrorResponseFactory
{
    public static ApiErrorResponse Create(
        HttpContext httpContext,
        int status,
        string message,
        IReadOnlyList<FieldViolationResponse>? violations = null,
        string? error = null,
        string? path = null) =>
        new(
            DateTimeOffset.UtcNow,
            status,
            error ?? ReasonPhrases.GetReasonPhrase(status),
            message,
            path ?? httpContext.Request.Path,
            violations ?? []);

    public static string ToJsonFieldName(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return field;
        }

        return string.Join(
            ".",
            field.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }
}