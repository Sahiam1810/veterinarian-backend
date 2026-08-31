using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Api.Common.Security;

public static class JwtResponseEvents
{
    private const string NotificationsHubPath = "/hubs/notifications";

    public static JwtBearerEvents Create() => new()
    {
        // El cliente de SignalR no puede mandar el header Authorization en el
        // handshake de WebSocket, así que manda el token por query string.
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments(NotificationsHubPath))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await WriteProblemAsync(
                context.Response,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication.Unauthorized",
                context.HttpContext.RequestAborted);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await WriteProblemAsync(
                context.Response,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "Authentication.Forbidden",
                context.HttpContext.RequestAborted);
        }
    };

    private static Task WriteProblemAsync(
        HttpResponse response,
        int status,
        string title,
        string code,
        CancellationToken cancellationToken) =>
        JsonSerializer.SerializeAsync(
            response.Body,
            new
            {
                type = $"https://httpstatuses.com/{status}",
                title,
                status,
                code
            },
            cancellationToken: cancellationToken);
}