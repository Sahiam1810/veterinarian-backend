using FluentValidation;
using Application.Agent.Errors;
using Application.Common.Exceptions;
using Application.Telegram.Errors;
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
        var (status, message, agentError) = Map(exception);
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
        } : agentError;
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

    private static (int Status, string Message, string? AgentError) Map(Exception exception) =>
        exception switch
        {
            AgentConversationNotFoundException notFound =>
                (StatusCodes.Status404NotFound,
                    notFound.Message,
                    "agent_conversation_not_found"),
            AgentConversationForbiddenException forbidden =>
                (StatusCodes.Status403Forbidden,
                    forbidden.Message,
                    "agent_conversation_forbidden"),
            AgentConversationConfigurationException configuration =>
                (StatusCodes.Status503ServiceUnavailable,
                    configuration.Message,
                    "agent_conversation_configuration_error"),
            AgentAuthenticationException authentication =>
                (StatusCodes.Status502BadGateway, authentication.Message, "agent_authentication_error"),
            AgentContractException contract =>
                (StatusCodes.Status502BadGateway, contract.Message, "agent_contract_error"),
            AgentIdempotencyConflictException conflict =>
                (StatusCodes.Status409Conflict, conflict.Message, "agent_idempotency_conflict"),
            AgentUnavailableException unavailable =>
                (StatusCodes.Status503ServiceUnavailable, unavailable.Message, "agent_unavailable"),
            AgentTimeoutException timeout =>
                (StatusCodes.Status504GatewayTimeout, timeout.Message, "agent_timeout"),
            TelegramAccountUnavailableException unavailable =>
                (StatusCodes.Status403Forbidden, unavailable.Message, "telegram_account_unavailable"),
            TelegramIdentityConflictException conflict =>
                (StatusCodes.Status409Conflict, conflict.Message, "telegram_identity_conflict"),
            TelegramLinkCodeInvalidException invalidCode =>
                (StatusCodes.Status400BadRequest, invalidCode.Message, "telegram_link_code_invalid"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", null),
            BadRequestException badRequest => (StatusCodes.Status400BadRequest, badRequest.Message, null),
            ArgumentException argument => (StatusCodes.Status400BadRequest, argument.Message, null),
            UnauthorizedException unauthorized => (StatusCodes.Status401Unauthorized, unauthorized.Message, null),
            UnauthorizedAccessException unauthorizedAccess => (StatusCodes.Status401Unauthorized, unauthorizedAccess.Message, null),
            ForbiddenException forbidden => (StatusCodes.Status403Forbidden, forbidden.Message, null),
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message, null),
            KeyNotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message, null),
            ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message, conflict.Code),
            DbUpdateException => (StatusCodes.Status409Conflict, "Data integrity violation", null),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", null)
        };
}
