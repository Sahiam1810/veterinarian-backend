using System.Text.Json;
using Api.Common.Errors;
using Application.Clients.Errors;
using Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Api.Tests.Owners;

// Regresión: el contrato problem+json+code solo aplica a POST /api/owners/bot exacto.
public sealed class BotOwnerRegistrationErrorContractTests
{
    [Fact]
    public async Task Conflict_OnExactPostBotPath_UsesProblemJsonCode()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateContext(HttpMethods.Post, "/api/owners/bot");

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

    [Theory]
    [InlineData("POST", "/api/owners/bot/extra")]
    [InlineData("POST", "/api/owners/bot/")]
    [InlineData("GET", "/api/owners/bot")]
    [InlineData("PUT", "/api/owners/bot")]
    public async Task Conflict_OnNonExactBotRoute_KeepsLegacyErrorField(
        string method,
        string path)
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateContext(method, path);

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ConflictException(
                "Ya existe un cliente con ese número de teléfono.",
                ClientErrorCodes.PhoneAlreadyInUse),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal(
            ClientErrorCodes.PhoneAlreadyInUse,
            document.RootElement.GetProperty("error").GetString());
        Assert.False(document.RootElement.TryGetProperty("code", out _));
        Assert.NotEqual(
            "application/problem+json",
            httpContext.Response.ContentType);
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
