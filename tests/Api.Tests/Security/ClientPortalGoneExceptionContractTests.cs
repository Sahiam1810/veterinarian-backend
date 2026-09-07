using System.Text.Json;
using Api.Common.Errors;
using Application.Common.Exceptions;
using Application.Security.Errors;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Api.Tests.Security;

// Contrato HTTP 410 para rutas de portal Cliente retiradas.
public sealed class ClientPortalGoneExceptionContractTests
{
    [Fact]
    public async Task GoneException_with_ClientPortalGone_emits_problem_json_code()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/clients/me";
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new GoneException(ClientPortalErrors.Gone),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status410Gone, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        var root = document.RootElement;

        Assert.Equal(ClientPortalErrors.Gone.Code, root.GetProperty("code").GetString());
        Assert.Equal(410, root.GetProperty("status").GetInt32());
        Assert.Equal("Gone", root.GetProperty("title").GetString());
        Assert.True(root.TryGetProperty("type", out _));
        Assert.False(root.TryGetProperty("message", out _));
        Assert.False(root.TryGetProperty("detail", out _));
    }
}