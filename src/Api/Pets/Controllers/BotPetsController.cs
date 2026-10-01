using Api.Common.Security;
using Api.Pets.Dtos;
using Api.Pets.Mappings;
using Application.Pets.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Pets.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TelegramAgentOnly)]
[Route("api/bot/pets")]
public sealed class BotPetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene las mascotas para el agente de Telegram")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OwnedPetProfileResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<OwnedPetProfileResponseDto>>> GetOwned(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetClientId(out var clientId))
        {
            return Unauthorized();
        }

        var pets = await sender.Send(new GetMyPetsQuery(clientId), cancellationToken);
        return Ok(pets.Select(pet => pet.ToDto()).ToArray());
    }

    [HttpPost("query-by-claim-proof")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ContactEmailConfirm)]
    [EndpointSummary("Lista mascotas con proof Claim temporal (sin bot-link)")]
    [EndpointDescription(
        "Consume el proof de un solo uso emitido tras OTP Claim. No crea TELEGRAM_USER_LINKS. "
        + "Pensado para consulta desde Telegram invitado (celular prestado).")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OwnedPetProfileResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IReadOnlyCollection<OwnedPetProfileResponseDto>>> QueryByClaimProof(
        [FromBody] QueryPetsByClaimProofRequest request,
        CancellationToken cancellationToken)
    {
        var pets = await sender.Send(
            new QueryPetsByClaimProof(request.SessionId, request.Proof),
            cancellationToken);
        return Ok(pets.Select(pet => pet.ToDto()).ToArray());
    }

    [HttpPost]
    [EndpointSummary("Registra una mascota desde el agente de Telegram")]
    [ProducesResponseType(typeof(OwnedPetProfileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnedPetProfileResponseDto>> Register(
        [FromBody] CreateOwnedPetDto request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetClientId(out var clientId))
        {
            return Unauthorized();
        }

        var profile = await sender.Send(
            new RegisterMyPetCommand(
                clientId,
                request.Name,
                request.Age,
                request.Gender,
                request.Weight,
                request.Observations,
                request.SpeciesId,
                request.RaceId),
            cancellationToken);

        return Created($"/api/bot/pets/{profile.Id}", profile.ToDto());
    }

    [HttpPatch("{petId:guid}")]
    [EndpointSummary("Actualiza una mascota desde el agente de Telegram")]
    [ProducesResponseType(typeof(OwnedPetProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OwnedPetProfileResponseDto>> Update(
        Guid petId,
        [FromBody] UpdateOwnedPetProfileDto request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetClientId(out var clientId))
        {
            return Unauthorized();
        }

        var profile = await sender.Send(
            new UpdateMyPetProfileCommand(
                clientId,
                petId,
                request.Name,
                request.Age,
                request.Gender,
                request.Weight,
                request.Observations,
                request.ChangeObservations,
                request.SpeciesId,
                request.RaceId,
                request.ExpectedUpdatedAt),
            cancellationToken);

        return Ok(profile.ToDto());
    }
}
