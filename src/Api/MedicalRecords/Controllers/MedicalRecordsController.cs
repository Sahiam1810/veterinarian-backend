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
    [HttpGet]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene todas las historias médicas")]
    [EndpointDescription("Retorna el listado completo de historias médicas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicalRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalRecordResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await sender.Send(
            new GetMedicalRecordByIdQuery(id),
            cancellationToken);

        return Ok(record.ToResponse());
    }
}
