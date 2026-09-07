using System.Text.Json;
using Api.Auth.Dtos;
using Api.Common.Security;
using Application.Security.EmailOtp;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Auth.Controllers;

[ApiController]
[Route("api/auth/email-otp")]
public sealed class EmailAuthController(ISender sender) : ControllerBase
{
    private ContentResult AuthProblem(int status, string title, string code) => new()
    {
        StatusCode = status,
        ContentType = "application/problem+json",
        Content = JsonSerializer.Serialize(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            code
        })
    };

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.EmailOtpRequest)]
    [HttpPost("request")]
    [EndpointSummary("Solicita código OTP de autenticación por correo")]
    [EndpointDescription("Envía un código OTP al correo provisto si es válido. No expone el código generado ni filtra existencia de cuentas.")]
    [ProducesResponseType(typeof(RequestEmailOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] RequestEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RequestEmailOtpCommand(request.Email),
            cancellationToken);

        if (result.IsFailure)
        {
            return AuthProblem(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                result.Error.Code);
        }

        return Ok(new RequestEmailOtpResponse());
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.EmailOtpConfirm)]
    [HttpPost("confirm")]
    [EndpointSummary("Confirma código OTP e inicia sesión")]
    [EndpointDescription("Valida el código OTP recibido por correo y retorna los tokens de acceso y refresco.")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ConfirmOtp(
        [FromBody] ConfirmEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ConfirmEmailOtpCommand(request.Email, request.Code),
            cancellationToken);

        if (result.IsFailure)
        {
            return AuthProblem(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                result.Error.Code);
        }

        return Ok(AuthenticationResponse.From(result.Value));
    }
}
