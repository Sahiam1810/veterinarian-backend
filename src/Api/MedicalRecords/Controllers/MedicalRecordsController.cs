using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.MedicalRecords.Dtos;
using Api.MedicalRecords.Mappings;
using Application.MedicalRecords.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.MedicalRecords.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MedicalRecordsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Historiales Clínicos", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo registro de historia médica")]
    [EndpointDescription("Registra una nueva historia médica para la mascota de un cliente. Una vez creada, la historia clínica queda inmutable como parte del historial de la mascota.")]
    [ProducesResponseType(typeof(CreateMedicalRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateMedicalRecordResponse>> Create(
        [FromBody] CreateMedicalRecordRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateMedicalRecordResponse(id));
    }

    [HttpGet]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene todas las historias médicas")]
    [EndpointDescription("Retorna el listado completo de historias médicas registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicalRecordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MedicalRecordResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var records = await sender.Send(
            new GetAllMedicalRecordsQuery(),
            cancellationToken);

        return Ok(records.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene una historia médica por su ID")]
    [EndpointDescription("Retorna la información detallada de una historia médica específica.")]
    [ProducesResponseType(typeof(MedicalRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalRecordResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await sender.Send(
            new GetMedicalRecordByIdQuery(id),
            cancellationToken);

        return record is null
            ? NotFound()
            : Ok(record.ToResponse());
    }
}
