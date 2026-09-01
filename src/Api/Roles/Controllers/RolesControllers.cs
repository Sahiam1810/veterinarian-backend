using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Roles.Dtos;
using Api.Roles.Mappings;
using Application.Roles.UseCase;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Roles.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Roles", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo rol de usuario")]
    [EndpointDescription("Registra un nuevo rol (ej. Administrador, Agente, Cliente) en el sistema.")]
    [ProducesResponseType(typeof(CreateRoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateRoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var roleId = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateRoleResponse(roleId));
    }

    [HttpGet]
    [RequirePermission("Roles", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los roles")]
    [EndpointDescription("Retorna el listado de todos los roles configurados en la plataforma.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var roles = await sender.Send(
            new GetAllRolesQuery(),
            cancellationToken);

        return Ok(roles.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Roles", PermissionAction.View)]
    [EndpointSummary("Obtiene un rol por su ID")]
    [EndpointDescription("Retorna la información de un rol específico por su identificador GUID.")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var role = await sender.Send(
            new GetRoleByIdQuery(id),
            cancellationToken);

        return Ok(role.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Roles", PermissionAction.Edit)]
    [EndpointSummary("Actualiza los datos de un rol")]
    [EndpointDescription("Modifica el nombre o la descripción de un rol de usuario existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Roles", PermissionAction.Delete)]
    [EndpointSummary("Elimina un rol por su ID")]
    [EndpointDescription("Remueve un rol de usuario del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteRoleCommand(id),
            cancellationToken);

        return NoContent();
    }
}