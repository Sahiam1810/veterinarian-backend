using Api.Common.Security;
using Api.Telegram.Dtos;
using Api.Telegram.Security;
using Application.Telegram.Updates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Telegram.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/integrations/telegram/webhook")]
[EnableRateLimiting(RateLimitPolicies.TelegramWebhook)]
public sealed class TelegramWebhookController(
    ISender sender,
    ITelegramWebhookSecretValidator secretValidator) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Recibe actualizaciones del bot de Telegram")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(
        [FromBody] TelegramUpdateRequest update,
        [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string? secret,
        CancellationToken cancellationToken)
    {
        if (!secretValidator.IsValid(secret))
        {
            return Unauthorized();
        }

        if (update.Message is not { From: not null } message)
        {
            return Ok();
        }

        await sender.Send(
            new IngestTelegramUpdateCommand(
                update.UpdateId,
                message.From.Id,
                message.Chat.Id,
                message.MessageId,
                message.Chat.Type,
                message.Text),
            cancellationToken);
        return Ok();
    }
}
