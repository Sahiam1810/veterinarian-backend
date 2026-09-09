using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Api.Configuration;

public sealed class SwaggerBasicAuthMiddleware
{
    private const string SwaggerPathPrefix = "/swagger";
    private readonly RequestDelegate _next;

    public SwaggerBasicAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (!context.Request.Path.StartsWithSegments(SwaggerPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var expectedUser = configuration["SWAGGER_USER"]
            ?? throw new InvalidOperationException("SWAGGER_USER no está configurado.");
        var expectedPassword = configuration["SWAGGER_PASSWORD"]
            ?? throw new InvalidOperationException("SWAGGER_PASSWORD no está configurado.");


        if (context.Request.Headers.TryGetValue("Authorization", out var authHeaderValues) &&
            AuthenticationHeaderValue.TryParse(authHeaderValues.ToString(), out var authHeader) &&
            string.Equals(authHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            try
            {
                var credentialsBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialsBytes).Split(':', 2);

                if (credentials.Length == 2)
                {
                    var username = credentials[0];
                    var password = credentials[1];

                    if (string.Equals(username, expectedUser, StringComparison.Ordinal) &&
                        string.Equals(password, expectedPassword, StringComparison.Ordinal))
                    {
                        await _next(context);
                        return;
                    }
                }
            }
            catch (FormatException)
            {
                // In case of invalid base64 encoding, fall through to 401
            }
        }

        context.Response.Headers.WWWAuthenticate = "Basic realm=\"Swagger UI\", charset=\"UTF-8\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Se requiere autenticación para acceder a la documentación de Swagger.");
    }
}
