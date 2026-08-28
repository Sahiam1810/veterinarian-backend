using System.Security.Claims;
using Api.Agent.Dtos;
using Application.Agent.Messages;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Agent.Controllers;

[ApiController]
[Authorize]
[Route("api/agent/messages")]
public sealed class AgentMessagesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Envía un mensaje al agente conversacional")]
    [EndpointDescription("Deriva la identidad del JWT y reenvía el mensaje al servicio interno del agente.")]
    [ProducesResponseType(typeof(SendAgentMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<SendAgentMessageResponse>> Send(
        [FromBody] SendAgentMessageRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var personClaim = User.FindFirstValue("person_id");
        var role = User.FindFirstValue("role");
        if (!Guid.TryParse(personClaim, out var personId) || personId == Guid.Empty ||
            string.IsNullOrWhiteSpace(role))
        {
            throw new UnauthorizedException("Authenticated identity is invalid.");
        }

        var result = await sender.Send(
            new SendAgentMessageCommand(
                request.Message,
                request.ConversationId,
                request.PetId,
                request.Language,
                personId,
                role,
                idempotencyKey,
                correlationId ?? Guid.NewGuid()),
            cancellationToken);

        return Ok(new SendAgentMessageResponse(
            result.Message,
            result.ConversationId,
            result.CorrelationId,
            result.ResponseType,
            result.Module));
    }
}
