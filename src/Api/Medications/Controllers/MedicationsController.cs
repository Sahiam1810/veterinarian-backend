using Api.Common.Security.Permissions;
using Api.Medications.Dtos;
using Api.Medications.Mappings;
using Application.Medications.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Medications.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MedicationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los medicamentos del catálogo")]
    [ProducesResponseType(typeof(IEnumerable<MedicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MedicationDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var medications = await sender.Send(new GetAllMedicationsQuery(onlyActive), cancellationToken);
        return Ok(medications.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.View)]
    [EndpointSummary("Obtiene un medicamento por su ID")]
    [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var medication = await sender.Send(new GetMedicationByIdQuery(id), cancellationToken);
        return Ok(medication.ToResponse());
    }

    [HttpPost]
    [RequirePermission("Órdenes Médicas", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo medicamento en el catálogo")]
    [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MedicationDto>> Create(
        [FromBody] CreateMedicationDto dto,
        CancellationToken cancellationToken = default)
    {
        var medication = await sender.Send(dto.ToCommand(), cancellationToken);
        var response = medication.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza un medicamento existente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateMedicationDto dto,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(dto.ToCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Órdenes Médicas", PermissionAction.Delete)]
    [EndpointSummary("Desactiva un medicamento del catálogo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeleteMedicationCommand(id), cancellationToken);
        return NoContent();
    }
}
