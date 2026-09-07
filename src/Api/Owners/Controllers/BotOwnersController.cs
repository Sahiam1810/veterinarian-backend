using Api.Common.Security;
using Api.Owners.Dtos;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Owners.Controllers;

// Etapa 4.2: alta anónima de dueño para chatbot. Solo despacha a IRegisterOwnerFromBot.
[ApiController]
[Route("api/owners/bot")]
public sealed class BotOwnersController(IRegisterOwnerFromBot registerOwner) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.BotOwnerRegistration)]
    [EndpointSummary("Registra un dueño desde el chatbot (anónimo)")]
    [EndpointDescription(
        "Endpoint anónimo rate-limited para bot. Exige proof Email (sessionId + proof). " +
        "RequireContactProofs queda forzado por el canal Bot en Application; no se acepta desde el body. " +
        "No envía OTP ni crea password/USER_ACCOUNTS: el login web de ese email sigue denegado. " +
        "Tras 201, el Client es localizable por los lookups Etapa 2 (cédula/teléfono) con los mismos datos. " +
        "Errores de negocio: application/problem+json con `code` estable (400/409/429). " +
        "El front/bot mapea por code, no por mensajes ni formato legacy.")]
    [ProducesResponseType(typeof(RegisterOwnerBotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RegisterOwnerBotResponse>> Register(
        [FromBody] RegisterOwnerBotRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ContactProofSessionId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.ContactProof))
        {
            // Misma vía problem+json+code que el núcleo (ContactVerificationException).
            throw new ContactVerificationException(OwnerRegistrationErrors.ProofRequired);
        }

        var result = await registerOwner.RegisterAsync(
            new RegisterOwnerFromBotRequest(
                request.FullName,
                request.Email,
                request.IdentificationNumber,
                request.PhoneNumber,
                request.ContactProofSessionId,
                request.ContactProof,
                request.Address),
            cancellationToken);

        var response = new RegisterOwnerBotResponse(result.UserId, result.ClientId);
        return Created($"/api/owners/bot/{result.ClientId}", response);
    }
}
