using System.Text.Json;
using Api.Common.Errors;
using Application.Common.Exceptions;
using Application.Security.Errors;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Api.Tests.Security;

/// <summary>
/// Contrato HTTP de bloqueo Cliente en create/update account/credential:
/// ForbiddenException(Error) → 403 application/problem+json con code estable.
/// </summary>
public sealed class PlatformAccessDeniedForbiddenExceptionContractTests
{
    [Fact]
    public async Task ForbiddenException_with_PlatformAccessDenied_emits_problem_json_code()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/UserAccounts";
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ForbiddenException(AuthenticationErrors.PlatformAccessDenied),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        var root = document.RootElement;

        Assert.Equal(
            AuthenticationErrors.PlatformAccessDenied.Code,
            root.GetProperty("code").GetString());
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", root.GetProperty("title").GetString());

        var payload = root.GetRawText();
        Assert.DoesNotContain("cliente@", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cliente", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detail", payload, StringComparison.OrdinalIgnoreCase);
        Assert.False(root.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task ForbiddenException_without_code_keeps_legacy_ApiErrorResponse()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/appointments/x";
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ForbiddenException("La cita no está asignada al veterinario autenticado."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal("Forbidden", document.RootElement.GetProperty("error").GetString());
        Assert.False(document.RootElement.TryGetProperty("code", out _));
    }
}
