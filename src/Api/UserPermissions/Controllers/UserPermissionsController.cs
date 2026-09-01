using Api.Common.Security;
using Api.UserPermissions.Dtos;
using Api.UserPermissions.Mappings;
using Application.UserPermissions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.UserPermissions.Controllers;

[ApiController]
[Route("api/user-permissions")]
public sealed class UserPermissionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene los permisos puntuales por usuario")]
    [EndpointDescription("Lista todos los permisos puntuales configurados por usuario y módulo.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserPermissionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserPermissionResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllUserPermissionsQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene un permiso puntual")]
    [EndpointDescription("Busca un permiso puntual por su identificador.")]
    [ProducesResponseType(typeof(UserPermissionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPermissionResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetUserPermissionByIdQuery(id), ct)).ToDto());

    [HttpGet("by-user/{userId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene los permisos puntuales de un usuario")]
    [EndpointDescription("Lista los permisos puntuales asociados a un usuario específico.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserPermissionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserPermissionResponseDto>>> GetByUserId(
        Guid userId,
        CancellationToken ct) =>
        Ok((await sender.Send(new GetUserPermissionsByUserIdQuery(userId), ct)).Select(x => x.ToDto()).ToArray());

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Crea un permiso puntual")]
    [EndpointDescription("Asigna a un usuario específico un permiso extra sobre un módulo, además del que ya le da su rol.")]
    [ProducesResponseType(typeof(UserPermissionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserPermissionResponseDto>> Create(
        CreateUserPermissionDto dto,
        CancellationToken ct)
    {
        var id = await sender.Send(
            new CreateUserPermissionCommand(
                dto.UserId,
                dto.ModuleId,
                dto.CanView,
                dto.CanCreate,
                dto.CanEdit,
                dto.CanDelete),
            ct);
        var permission = await sender.Send(new GetUserPermissionByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, permission.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Actualiza un permiso puntual")]
    [EndpointDescription("Actualiza los flags CanView/CanCreate/CanEdit/CanDelete.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateUserPermissionDto dto, CancellationToken ct)
    {
        await sender.Send(
            new UpdateUserPermissionCommand(
                id,
                dto.CanView,
                dto.CanCreate,
                dto.CanEdit,
                dto.CanDelete),
            ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Elimina un permiso puntual")]
    [EndpointDescription("Elimina un permiso puntual por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteUserPermissionCommand(id), ct);
        return NoContent();
    }
}
