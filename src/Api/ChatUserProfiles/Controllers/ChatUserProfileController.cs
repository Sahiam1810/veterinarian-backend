using Api.ChatUserProfiles.Dtos;
using Api.ChatUserProfiles.Mappings;
using Api.Common.Security;
using Application.ChatUserProfiles.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ChatUserProfiles.Controllers;

[ApiController]
[Route("api/chat/user-profiles")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ChatUserProfileController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un perfil de chat")]
    [EndpointDescription("Registra un perfil de chat para un usuario existente. Un usuario puede tener varios perfiles.")]
    [ProducesResponseType(typeof(ChatUserProfileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatUserProfileResponseDto>> Create(
        [FromBody] CreateChatUserProfileDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = profile.ToResponse();
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
    [EndpointSummary("Listar perfiles de chat")]
    [EndpointDescription("Devuelve todos los perfiles de chat registrados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatUserProfileResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatUserProfileResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var profiles = await sender.Send(new GetAllChatUserProfilesQuery(), cancellationToken);
        return Ok(profiles.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener un perfil de chat por su identificador")]
    [EndpointDescription("Devuelve el perfil de chat indicado.")]
    [ProducesResponseType(typeof(ChatUserProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatUserProfileResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var profile = await sender.Send(new GetChatUserProfileByIdQuery(id), cancellationToken);
        return profile is null
            ? NotFound(new { Message = $"No se encontró el perfil de chat '{id}'." })
            : Ok(profile.ToResponse());
    }

    [HttpGet("by-user/{userId:guid}")]
    [EndpointSummary("Consultar perfiles de chat por usuario")]
    [EndpointDescription("Devuelve todos los perfiles de chat asociados al usuario indicado. Si no hay registros, responde con una lista vacía.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ChatUserProfileResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ChatUserProfileResponseDto>>> GetByUserId(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profiles = await sender.Send(new GetChatUserProfilesByUserIdQuery(userId), cancellationToken);
        return Ok(profiles.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar un perfil de chat")]
    [EndpointDescription("Actualiza el nombre visible, el avatar y la biografía. No modifica el usuario asociado.")]
    [ProducesResponseType(typeof(ChatUserProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatUserProfileResponseDto>> Update(
        Guid id,
        [FromBody] UpdateChatUserProfileDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(profile.ToResponse());
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
    [EndpointSummary("Eliminar un perfil de chat")]
    [EndpointDescription("Elimina el perfil de chat. No hay participantes de conversación que lo referencien todavía.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteChatUserProfileCommand(id), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
