using System.Text.Json;
using Api.Common.Errors;
using Application.Clients.Errors;
using Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Api.Tests.Clients;

// Frente 1: correo y cédula duplicados responden 409 con su código estable Clients.*
// (el frontend y el chatbot mapean por code, no por el mensaje).
public sealed class ClientConflictCodesHttpMappingTests
{
    [Theory]
    [InlineData("/api/clients", ClientErrorCodes.EmailAlreadyInUse, "Clients.EmailAlreadyInUse")]
    [InlineData("/api/clients", ClientErrorCodes.IdentificationAlreadyInUse, "Clients.IdentificationAlreadyInUse")]
    [InlineData("/api/owners/bot", ClientErrorCodes.EmailAlreadyInUse, "Clients.EmailAlreadyInUse")]
    [InlineData("/api/owners/bot", ClientErrorCodes.IdentificationAlreadyInUse, "Clients.IdentificationAlreadyInUse")]
    public async Task Duplicate_email_or_identification_maps_to_409_with_stable_clients_code(
        string path, string code, string expectedLiteral)
    {
        Assert.Equal(expectedLiteral, code);
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ConflictException("duplicado", code),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal(StatusCodes.Status409Conflict, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedLiteral, document.RootElement.GetProperty("code").GetString());
    }
}
