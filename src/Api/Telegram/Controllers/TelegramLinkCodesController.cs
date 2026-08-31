using System.Security.Claims;
using Api.Telegram.Dtos;
using Application.Common.Exceptions;
using Application.Telegram.Linking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Telegram.Controllers;

[ApiController]
[Authorize]
[Route("api/integrations/telegram/link-codes")]
public sealed class TelegramLinkCodesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Genera un código temporal para vincular Telegram")]
    [ProducesResponseType(typeof(CreateTelegramLinkCodeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateTelegramLinkCodeResponse>> Create(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("person_id"), out var personId) || personId == Guid.Empty)
        {
            throw new UnauthorizedException("Authenticated identity is invalid.");
        }

        var result = await sender.Send(
            new CreateTelegramLinkCodeCommand(personId),
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            new CreateTelegramLinkCodeResponse(result.Code, result.DeepLink, result.ExpiresAt));
    }
}
