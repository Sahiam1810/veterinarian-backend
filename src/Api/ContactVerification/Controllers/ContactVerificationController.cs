using Api.Common.Security;
using Api.ContactVerification.Dtos;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Domain.ContactVerification.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.ContactVerification.Controllers;

// Etapa 3: Request Email (3.1) + Confirm proof (3.2). RegisterOwner fuera.
[ApiController]
[Route("api/contact-verification")]
public sealed class ContactVerificationController(
    IRequestContactEmailVerification requestEmail,
    IConfirmContactEmailVerification confirmEmail,
    IRequestClaimEmailByIdentification requestClaimByIdentification) : ControllerBase
{
    [HttpPost("email/request")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ContactEmailRequest)]
    [EndpointSummary("Solicita OTP de contacto por correo")]
    [EndpointDescription("Canal v1 solo Email. Purpose Register. Devuelve sessionId/expiresAt/channel sin OTP.")]
    [ProducesResponseType(typeof(RequestContactEmailVerificationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RequestContactEmailVerificationResponse>> RequestEmail(
        [FromBody] RequestContactEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParsePurpose(request.Purpose, out var purpose))
        {
            return BadRequest(new { code = ContactVerificationErrors.PurposeInvalid.Code });
        }

        var result = await requestEmail.RequestAsync(
            new RequestContactEmailVerification(request.Email, purpose),
            cancellationToken);

        return Accepted(new RequestContactEmailVerificationResponse(
            result.SessionId,
            result.ExpiresAt,
            result.Channel.ToString()));
    }

    [HttpPost("email/request-claim-by-identification")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ContactEmailRequest)]
    [EndpointSummary("Solicita OTP Claim resolviendo el correo por cédula")]
    [EndpointDescription(
        "Para invitados de Telegram que solo envían cédula. Busca el cliente, envía OTP Claim "
        + "al correo registrado y responde metadatos seguros (maskedEmail) sin OTP ni email crudo.")]
    [ProducesResponseType(typeof(RequestClaimEmailByIdentificationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RequestClaimEmailByIdentificationResponse>> RequestClaimByIdentification(
        [FromBody] RequestClaimEmailByIdentificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await requestClaimByIdentification.RequestAsync(
            new RequestClaimEmailByIdentification(request.IdentificationNumber),
            cancellationToken);

        return Accepted(new RequestClaimEmailByIdentificationResponse(
            result.SessionId,
            result.ExpiresAt,
            result.Channel.ToString(),
            result.MaskedEmail));
    }

    [HttpPost("email/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ContactEmailConfirm)]
    [EndpointSummary("Confirma OTP de contacto y emite proof de un solo uso")]
    [EndpointDescription("Emite proof single-use; no lo consume ni registra dueño. Codes tipados vía ContactVerificationException.")]
    [ProducesResponseType(typeof(ConfirmContactEmailVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ConfirmContactEmailVerificationResponse>> ConfirmEmail(
        [FromBody] ConfirmContactEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await confirmEmail.ConfirmAsync(
            new ConfirmContactEmailVerification(request.SessionId, request.Code),
            cancellationToken);

        return Ok(new ConfirmContactEmailVerificationResponse(result.SessionId, result.Proof));
    }

    private static bool TryParsePurpose(string? purpose, out ContactVerificationPurpose parsed)
    {
        parsed = default;
        return Enum.TryParse(purpose, ignoreCase: true, out parsed)
            && parsed == ContactVerificationPurpose.Register;
    }
}
