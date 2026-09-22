using Api.Common.Security;
using Api.Owners.Dtos;
using Application.Owners.Abstractions;
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
        "Endpoint anónimo rate-limited para bot. Por decisión de negocio, no exige " +
        "ni valida ningún código o proof de verificación. " +
        "Crea solo el cliente en CLIENTS, sin usuario ni contraseña: el cliente no accede a la plataforma. " +
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
        var result = await registerOwner.RegisterAsync(
            new RegisterOwnerFromBotRequest(
                request.FullName,
                request.Email,
                request.IdentificationNumber,
                request.PhoneNumber,
                request.Address),
            cancellationToken);

        var response = new RegisterOwnerBotResponse(result.ClientId);
        return Created($"/api/owners/bot/{result.ClientId}", response);
    }
}
