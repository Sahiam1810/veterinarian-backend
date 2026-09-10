using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Api.Tests.Security;

/// <summary>
/// Test-only helpers for constructing JWT variants without touching production code.
/// </summary>
internal static class JwtTestTokenBuilder
{
    public static string TamperPayloadClaim(string validToken, string claimName, string newValue)
    {
        var parts = validToken.Split('.');
        if (parts.Length != 3)
        {
            throw new ArgumentException("Expected a compact JWT.", nameof(validToken));
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var payload = JsonNode.Parse(payloadJson)?.AsObject()
            ?? throw new InvalidOperationException("JWT payload is not a JSON object.");
        payload[claimName] = newValue;

        var tamperedPayload = Base64UrlEncode(
            Encoding.UTF8.GetBytes(payload.ToJsonString(new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            })));

        return string.Join('.', parts[0], tamperedPayload, parts[2]);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var normalized = input.Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2:
                normalized += "==";
                break;
            case 3:
                normalized += "=";
                break;
        }

        return Convert.FromBase64String(normalized);
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
