using System.Text.Json;
using Api.Common.Errors;
using Application.Clients.Errors;
using Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Api.Tests.Clients;

public sealed class ClientPhoneConflictHttpMappingTests
{
    // Ruta real: DbUpdateException → UnitOfWork (OracleClientPhoneConflictMapper) → ConflictException → handler.
    [Fact]
    public async Task ConflictException_with_phone_code_maps_to_409_error_payload()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/clients";
        httpContext.Response.Body = new MemoryStream();

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
        Assert.Equal(StatusCodes.Status409Conflict, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            ClientErrorCodes.PhoneAlreadyInUse,
            document.RootElement.GetProperty("code").GetString());
        Assert.False(document.RootElement.TryGetProperty("error", out _));
        Assert.False(document.RootElement.TryGetProperty("message", out _));
    }
}
