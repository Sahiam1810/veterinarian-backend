using Api.Common.Security;
using Api.RolePermissions.Dtos;
using Api.RolePermissions.Mappings;
using Application.RolePermissions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.RolePermissions.Controllers;

[ApiController]
[Route("api/role-permissions")]
public sealed class RolePermissionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene los permisos de rol")]
    [EndpointDescription("Lista todos los permisos configurados por rol y módulo.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RolePermissionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RolePermissionResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllRolePermissionsQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene un permiso de rol")]
    [EndpointDescription("Busca un permiso por su identificador.")]
    [ProducesResponseType(typeof(RolePermissionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolePermissionResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetRolePermissionByIdQuery(id), ct)).ToDto());

    [HttpGet("by-role/{roleId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene permisos de un rol")]
    [EndpointDescription("Lista los permisos asociados a un rol.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RolePermissionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RolePermissionResponseDto>>> GetByRoleId(
        Guid roleId,
        CancellationToken ct) =>
        Ok((await sender.Send(new GetRolePermissionsByRoleIdQuery(roleId), ct)).Select(x => x.ToDto()).ToArray());

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Crea un permiso de rol")]
    [EndpointDescription("Asigna permisos de un rol sobre un módulo.")]
    [ProducesResponseType(typeof(RolePermissionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RolePermissionResponseDto>> Create(
        CreateRolePermissionDto dto,
        CancellationToken ct)
    {
        var id = await sender.Send(
            new CreateRolePermissionCommand(
                dto.RoleId,
                dto.ModuleId,
                dto.CanView,
                dto.CanCreate,
                dto.CanEdit,
                dto.CanDelete),
            ct);
        var permission = await sender.Send(new GetRolePermissionByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, permission.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Actualiza un permiso de rol")]
    [EndpointDescription("Actualiza los flags CanView/CanCreate/CanEdit/CanDelete.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateRolePermissionDto dto, CancellationToken ct)
    {
        await sender.Send(
            new UpdateRolePermissionCommand(
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
    [EndpointSummary("Elimina un permiso de rol")]
    [EndpointDescription("Elimina un permiso por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteRolePermissionCommand(id), ct);
        return NoContent();
    }
}
