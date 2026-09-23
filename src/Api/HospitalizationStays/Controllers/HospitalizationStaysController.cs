using System.Security.Claims;
using Api.Common.Security.Permissions;
using Application.HospitalizationStays.UseCases;
using Domain.HospitalizationStays.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.HospitalizationStays.Controllers;

[ApiController]
[Route("api/hospitalization-stays")]
public sealed class HospitalizationStaysController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Hospitalización", PermissionAction.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Admit([FromBody] AdmitHospitalizationStayRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var id = await sender.Send(new AdmitHospitalizationStayCommand(request.ClientPetId, request.AppointmentId, request.Motivo, userId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPatch("{id:guid}/discharge")]
    [RequirePermission("Hospitalización", PermissionAction.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Discharge(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DischargeHospitalizationStayCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(HospitalizationStay), StatusCodes.Status200OK)]
    public async Task<ActionResult<HospitalizationStay>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var stay = await sender.Send(new GetHospitalizationStayByIdQuery(id), cancellationToken);
        return Ok(stay);
    }

    [HttpGet("active")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(IReadOnlyCollection<HospitalizationStay>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<HospitalizationStay>>> GetActive(CancellationToken cancellationToken)
    {
        var stays = await sender.Send(new GetAllActiveHospitalizationStaysQuery(), cancellationToken);
        return Ok(stays);
    }

    [HttpGet("pet/{clientPetId:guid}")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(IReadOnlyCollection<HospitalizationStay>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<HospitalizationStay>>> GetByPet(Guid clientPetId, CancellationToken cancellationToken)
    {
        var stays = await sender.Send(new GetHospitalizationStaysByPetQuery(clientPetId), cancellationToken);
        return Ok(stays);
    }

    [HttpPost("{id:guid}/notes")]
    [RequirePermission("Hospitalización", PermissionAction.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> AddNote(Guid id, [FromBody] AddHospitalizationNoteRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var noteId = await sender.Send(new AddHospitalizationNoteCommand(id, request.Nota, request.EntregadoAUserId, userId), cancellationToken);
        return CreatedAtAction(nameof(GetNotes), new { id }, noteId);
    }

    [HttpGet("{id:guid}/notes")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(IReadOnlyCollection<HospitalizationNote>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<HospitalizationNote>>> GetNotes(Guid id, CancellationToken cancellationToken)
    {
        var notes = await sender.Send(new GetHospitalizationNotesByStayQuery(id), cancellationToken);
        return Ok(notes);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("No se encontró el identificador del usuario autenticado.");

        return Guid.Parse(value);
    }

    public sealed record AdmitHospitalizationStayRequest(
        Guid ClientPetId,
        Guid? AppointmentId,
        string Motivo);

    public sealed record AddHospitalizationNoteRequest(
        string Nota,
        Guid? EntregadoAUserId);
}
