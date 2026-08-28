using FluentValidation;
using Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Api.Common.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, message) = Map(exception);
        var violations = exception is ValidationException validationException
            ? validationException.Errors
                .Select(failure => new FieldViolationResponse(
                    ApiErrorResponseFactory.ToJsonFieldName(failure.PropertyName),
                    failure.ErrorMessage))
                .ToArray()
            : [];
        var typeContract = httpContext.Request.Path.StartsWithSegments("/TypeContract", StringComparison.OrdinalIgnoreCase);
        if (typeContract && exception is ValidationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                timestamp = DateTimeOffset.UtcNow,
                status = StatusCodes.Status400BadRequest,
                error = "Bad Request",
                path = httpContext.Request.Path.ToString()
            }, cancellationToken);
            return true;
        }

        var error = typeContract ? status switch
        {
            StatusCodes.Status400BadRequest => "TYPE_CONTRACT_INVALID",
            StatusCodes.Status401Unauthorized => "TYPE_CONTRACT_UNAUTHORIZED",
            StatusCodes.Status404NotFound => "TYPE_CONTRACT_NOT_FOUND",
            StatusCodes.Status409Conflict => "TYPE_CONTRACT_CONFLICT",
            _ => null
        } : null;
        var path = typeContract ? $"uri={httpContext.Request.Path}" : null;

        httpContext.Response.StatusCode = status;

        var response = ApiErrorResponseFactory.Create(
            httpContext,
            status,
            message,
            violations,
            error,
            path);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static (int Status, string Message) Map(Exception exception) =>
        exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            BadRequestException badRequest => (StatusCodes.Status400BadRequest, badRequest.Message),
            ArgumentException argument => (StatusCodes.Status400BadRequest, argument.Message),
            UnauthorizedException unauthorized => (StatusCodes.Status401Unauthorized, unauthorized.Message),
            UnauthorizedAccessException unauthorizedAccess => (StatusCodes.Status401Unauthorized, unauthorizedAccess.Message),
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
            KeyNotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
            ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, "Data integrity violation"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };
}