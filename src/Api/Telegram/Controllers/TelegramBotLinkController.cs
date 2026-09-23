using System.Security.Claims;
using Api.Common.Security;
using Api.Telegram.Dtos;
using Application.Common.Exceptions;
using Application.Security.Claims;
using Application.Telegram.Linking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Telegram.Controllers;

// Ticket 2.5: cierra el "segundo turno" de la opción A (dos turnos) sin tocar
// ProcessTelegramUpdateHandler. Solo un token de invitado de Telegram (el que
// emite AgentDelegatedIdentityProvider.GetGuest) trae el claim telegram_user_id,
// así que el telegramUserId real nunca sale del backend ni pasa por el bot.
[ApiController]
[Authorize(Policy = AuthorizationPolicies.TelegramGuestLinkOnly)]
[Route("api/integrations/telegram/bot-link")]
public sealed class TelegramBotLinkController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Vincula el cliente registrado o encontrado por el bot con el Telegram invitado actual")]
    [ProducesResponseType(typeof(LinkTelegramBotAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LinkTelegramBotAccountResponse>> Link(
        [FromBody] LinkTelegramBotAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(
                User.FindFirstValue(DelegatedTokenClaims.TelegramUserId),
                out var telegramUserId) ||
            telegramUserId <= 0)
        {
            throw new UnauthorizedException("Authenticated identity is invalid.");
        }

        var linkId = await sender.Send(
            new LinkTelegramBotAccountCommand(request.ClientId, telegramUserId),
            cancellationToken);
        return Ok(new LinkTelegramBotAccountResponse(linkId));
    }

    [HttpPost("claim")]
    [EndpointSummary("Vincula el cliente del proof Claim con el Telegram invitado actual")]
    [ProducesResponseType(typeof(LinkTelegramBotAccountWithProofResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LinkTelegramBotAccountWithProofResponse>> Claim(
        [FromBody] LinkTelegramBotAccountWithProofRequest request,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(
                User.FindFirstValue(DelegatedTokenClaims.TelegramUserId),
                out var telegramUserId) ||
            telegramUserId <= 0)
        {
            throw new UnauthorizedException("Authenticated identity is invalid.");
        }

        var result = await sender.Send(
            new LinkTelegramBotAccountWithProofCommand(
                request.SessionId,
                request.Proof,
                telegramUserId),
            cancellationToken);
        return Ok(new LinkTelegramBotAccountWithProofResponse(result.LinkId, result.FullName));
    }
}
