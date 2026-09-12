using Api.Common.Security.Permissions;
using Api.VeterinarianAbsences.Dtos;
using Api.VeterinarianAbsences.Mappings;
using Application.VeterinarianAbsences.UseCases;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.VeterinarianAbsences.Controllers;

[ApiController]
[Route("api/veterinarian-absences")]
public sealed class VeterinarianAbsencesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Citas", PermissionAction.Create)]
    [EndpointSummary("Crea una ausencia de veterinario")]
    [EndpointDescription("Registra un bloqueo puntual del calendario. No sustituye el horario semanal recurrente.")]
    [ProducesResponseType(typeof(CreateVeterinarianAbsenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateVeterinarianAbsenceResponse>> Create(
        [FromBody] CreateVeterinarianAbsenceRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(request.ToCommand(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new CreateVeterinarianAbsenceResponse(id));
    }

    [HttpGet]
    [RequirePermission("Plataforma", PermissionAction.View)]
    [EndpointSummary("Obtiene todas las ausencias")]
    [EndpointDescription("Retorna el listado de ausencias de todos los veterinarios.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<VeterinarianAbsenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<VeterinarianAbsenceResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var absences = await sender.Send(new GetAllVeterinarianAbsencesQuery(), cancellationToken);
        return Ok(absences.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Plataforma", PermissionAction.View)]
    [EndpointSummary("Obtiene una ausencia por su ID")]
    [EndpointDescription("Retorna el detalle de una ausencia puntual.")]
    [ProducesResponseType(typeof(VeterinarianAbsenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeterinarianAbsenceResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var absence = await sender.Send(new GetVeterinarianAbsenceByIdQuery(id), cancellationToken);
        return Ok(absence.ToResponse());
    }

    [HttpGet("by-veterinarian/{veterinarianId:guid}")]
    [RequirePermission("Plataforma", PermissionAction.View)]
    [EndpointSummary("Obtiene las ausencias de un veterinario")]
    [EndpointDescription("Retorna las ausencias puntuales configuradas para un veterinario.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<VeterinarianAbsenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<VeterinarianAbsenceResponse>>> GetByVeterinarianId(
        Guid veterinarianId,
        CancellationToken cancellationToken)
    {
        var absences = await sender.Send(
            new GetVeterinarianAbsencesByVeterinarianIdQuery(veterinarianId),
            cancellationToken);
        return Ok(absences.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Citas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una ausencia existente")]
    [EndpointDescription("Modifica la ventana o el motivo de una ausencia puntual.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVeterinarianAbsenceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(request.ToCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Citas", PermissionAction.Delete)]
    [EndpointSummary("Elimina una ausencia por su ID")]
    [EndpointDescription("Remueve permanentemente una ausencia puntual del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteVeterinarianAbsenceCommand(id), cancellationToken);
        return NoContent();
    }
}
