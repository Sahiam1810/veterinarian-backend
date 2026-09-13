using Api.Clients.Dtos;
using Api.Common.Security;
using Application.Clients.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Clients.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TelegramBotChannel)]
[Route("api/bot/clients")]
public sealed class BotClientsController(ISender sender) : ControllerBase
{
    [HttpPost("find-or-create")]
    [EndpointSummary("Busca o registra un cliente por cédula para el agente de Telegram")]
    [EndpointDescription(
        "Find-or-create por IdentificationNumber única. Si existe, reutiliza y completa contacto vacío. " +
        "Si no, crea User + UserAccount + Client. Nunca duplica cédula. " +
        "TelegramUserId/ChatId son opcionales de correlación de sesión; no crean vínculo permanente. " +
        "Devuelve AccessToken delegado (token_use=telegram_agent) con TTL de idle (~5 min) para usar api/bot/*.")]
    [ProducesResponseType(typeof(FindOrCreateBotClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FindOrCreateBotClientResponse>> FindOrCreate(
        [FromBody] FindOrCreateBotClientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new FindOrCreateBotClientCommand(
                request.IdentificationNumber,
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.TelegramUserId,
                request.TelegramChatId),
            cancellationToken);

        return Ok(new FindOrCreateBotClientResponse(
            result.ClientId,
            result.UserId,
            result.UserAccountId,
            result.IdentificationNumber,
            result.Created,
            result.AccessToken));
    }
}
