using System.Text.Json;
using Api.Common.Errors;
using Application.Appointments.Errors;
using Application.Clients.Errors;
using Application.Common.Exceptions;
using Application.Security.Errors;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Api.Tests.Security;

// 6.3: excepciones con Code estable → problem+json (staff/Telegram traducen por code).
public sealed class StableErrorCodeContractTests
{
    [Fact]
    public async Task Conflict_WithStableCode_UsesProblemJson_OnAnyPath()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateContext(HttpMethods.Post, "/api/owners/bot/extra");

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ConflictException("Owner registration failed.", ClientErrorCodes.PhoneAlreadyInUse),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal(ClientErrorCodes.PhoneAlreadyInUse, document.RootElement.GetProperty("code").GetString());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Unauthorized_AppointmentActionPhoneMismatch_UsesProblemJsonCode()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateContext(HttpMethods.Post, "/api/appointments/x/request-code");

        var handled = await handler.TryHandleAsync(
            httpContext,
            new UnauthorizedException(
                "Phone mismatch.",
                AppointmentActionErrors.PhoneMismatch.Code),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal(
            AppointmentActionErrors.PhoneMismatch.Code,
            document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Conflict_WithoutCode_KeepsLegacyApiErrorResponse()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateContext(HttpMethods.Post, "/api/appointments");

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ConflictException("Slot taken."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.NotEqual("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.False(document.RootElement.TryGetProperty("code", out _));
    }

    [Fact]
    public void RateLimitExceeded_Code_IsStableConstant()
    {
        Assert.Equal("RateLimit.Exceeded", RateLimitErrors.Exceeded.Code);
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }
}
