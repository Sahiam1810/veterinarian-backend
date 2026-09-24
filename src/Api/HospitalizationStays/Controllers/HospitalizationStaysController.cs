using System.Security.Claims;
using Api.Common.Security.Permissions;
using Application.HospitalizationStays.Dtos;
using Application.HospitalizationStays.UseCases;
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

    [HttpPatch("{id:guid}/register-payment")]
    [RequirePermission("Hospitalización", PermissionAction.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPayment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RegisterHospitalizationStayPaymentCommand(id), cancellationToken);
        return NoContent();
    }


    [HttpGet("{id:guid}")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(ApiHospitalizationStayDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiHospitalizationStayDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var stay = await sender.Send(new GetHospitalizationStayByIdQuery(id), cancellationToken);
        return Ok(stay);
    }

    [HttpGet("active")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ApiHospitalizationStayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ApiHospitalizationStayDto>>> GetActive(CancellationToken cancellationToken)
    {
        var stays = await sender.Send(new GetAllActiveHospitalizationStaysQuery(), cancellationToken);
        return Ok(stays);
    }

    [HttpGet("admission-options")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(IReadOnlyCollection<HospitalizationAdmissionOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<HospitalizationAdmissionOptionDto>>> GetAdmissionOptions(CancellationToken cancellationToken)
    {
        var options = await sender.Send(new GetHospitalizationAdmissionOptionsQuery(), cancellationToken);
        return Ok(options);
    }

    [HttpGet("{id:guid}/liquidation")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(HospitalizationLiquidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HospitalizationLiquidationDto>> GetLiquidation(Guid id, CancellationToken cancellationToken)
    {
        var liquidation = await sender.Send(new GetHospitalizationLiquidationQuery(id), cancellationToken);
        return Ok(liquidation);
    }

    [HttpGet("pet/{clientPetId:guid}")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ApiHospitalizationStayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ApiHospitalizationStayDto>>> GetByPet(Guid clientPetId, CancellationToken cancellationToken)
    {
        var stays = await sender.Send(new GetHospitalizationStaysByPetQuery(clientPetId), cancellationToken);
        return Ok(stays);
    }

    [HttpGet("staff")]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(IReadOnlyCollection<HospitalizationStaffUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<HospitalizationStaffUserDto>>> GetStaff(CancellationToken cancellationToken)
    {
        var staff = await sender.Send(new GetHospitalizationStaffQuery(), cancellationToken);
        return Ok(staff);
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
    [ProducesResponseType(typeof(IReadOnlyCollection<ApiHospitalizationNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ApiHospitalizationNoteDto>>> GetNotes(Guid id, CancellationToken cancellationToken)
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
