using Api.ChatConversationAiSettings.Dtos;
using Api.ChatConversationAiSettings.Mappings;
using Api.Common.Security;
using Application.ChatConversationAiSettings.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatConversationAiSettings.Controllers;

[ApiController]
[Route("api/chat/conversation-ai-settings")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatConversationAiSettingController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear configuración de IA para una conversación")]
    [EndpointDescription("Registra la configuración de IA asociada a una conversación existente.")]
    [ProducesResponseType(typeof(ChatConversationAiSettingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationAiSettingResponseDto>> Create(
        [FromBody] CreateChatConversationAiSettingDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var setting = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = setting.ToResponse();
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpGet]
    [EndpointSummary("Listar configuraciones de IA")]
    [EndpointDescription("Devuelve todas las configuraciones de IA registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatConversationAiSettingResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatConversationAiSettingResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var settings = await sender.Send(new GetAllChatConversationAiSettingsQuery(), cancellationToken);
        return Ok(settings.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener configuración de IA por identificador")]
    [EndpointDescription("Devuelve la configuración de IA indicada.")]
    [ProducesResponseType(typeof(ChatConversationAiSettingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationAiSettingResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var setting = await sender.Send(new GetChatConversationAiSettingByIdQuery(id), cancellationToken);
        return setting is null
            ? NotFound(new { Message = $"No se encontró la configuración de IA '{id}'." })
            : Ok(setting.ToResponse());
    }

    [HttpGet("by-conversation/{conversationId:guid}")]
    [EndpointSummary("Consultar configuración de IA por conversación")]
    [EndpointDescription("Devuelve la configuración de IA más reciente asociada a la conversación indicada.")]
    [ProducesResponseType(typeof(ChatConversationAiSettingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationAiSettingResponseDto>> GetByConversationId(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var setting = await sender.Send(
            new GetChatConversationAiSettingByConversationIdQuery(conversationId),
            cancellationToken);

        return setting is null
            ? NotFound(new { Message = $"No se encontró configuración de IA para la conversación '{conversationId}'." })
            : Ok(setting.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar configuración de IA")]
    [EndpointDescription("Actualiza el estado de IA habilitada y el modelo por defecto.")]
    [ProducesResponseType(typeof(ChatConversationAiSettingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatConversationAiSettingResponseDto>> Update(
        Guid id,
        [FromBody] UpdateChatConversationAiSettingDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var setting = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(setting.ToResponse());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Eliminar configuración de IA")]
    [EndpointDescription("Elimina la configuración de IA indicada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteChatConversationAiSettingCommand(id), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
