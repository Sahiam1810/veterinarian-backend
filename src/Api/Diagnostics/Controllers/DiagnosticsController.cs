using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Diagnostics.Dtos;
using Api.Diagnostics.Mappings;
using Application.Diagnostics.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Diagnostics.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DiagnosticsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los diagnósticos")]
    [EndpointDescription("Retorna el listado de diagnósticos del catálogo clínico. Por defecto solo incluye los diagnósticos activos.")]
    [ProducesResponseType(typeof(IEnumerable<DiagnosticDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DiagnosticDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = await sender.Send(
            new GetAllDiagnosticsQuery(onlyActive),
            cancellationToken);

        return Ok(diagnostics.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene un diagnóstico por su ID")]
    [EndpointDescription("Retorna la información de un diagnóstico específico por su identificador GUID.")]
    [ProducesResponseType(typeof(DiagnosticDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiagnosticDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var diagnostic = await sender.Send(
            new GetDiagnosticByIdQuery(id),
            cancellationToken);

        return Ok(diagnostic.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Historiales Clínicos", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo diagnóstico")]
    [EndpointDescription("Registra un nuevo diagnóstico clínico (código único, nombre y descripción opcional) en el catálogo. Queda activo por defecto.")]
    [ProducesResponseType(typeof(DiagnosticDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiagnosticDto>> Create(
        [FromBody] CreateDiagnosticDto dto,
        CancellationToken cancellationToken = default)
    {
        var diagnostic = await sender.Send(
            dto.ToCommand(),
            cancellationToken);

        var response = diagnostic.ToResponse();

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Historiales Clínicos", PermissionAction.Edit)]
    [EndpointSummary("Actualiza un diagnóstico existente")]
    [EndpointDescription("Modifica el código, nombre, descripción o estado de un diagnóstico existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDiagnosticDto dto,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(
            dto.ToCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Historiales Clínicos", PermissionAction.Delete)]
    [EndpointSummary("Elimina (desactiva) un diagnóstico")]
    [EndpointDescription("Realiza una baja lógica del diagnóstico: lo marca como inactivo sin eliminarlo físicamente de la base de datos.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(
            new DeleteDiagnosticCommand(id),
            cancellationToken);

        return NoContent();
    }
}
