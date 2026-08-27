using Api.MedicalRecords.Dtos;
using Api.MedicalRecords.Mappings;
using Application.MedicalRecords.UseCases;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.MedicalRecords.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MedicalRecordsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crea un nuevo registro de historia médica")]
    [EndpointDescription("Registra una nueva historia médica para la mascota de un cliente.")]
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

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza una historia médica existente")]
    [EndpointDescription("Modifica los datos de una historia médica previamente registrada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMedicalRecordRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina una historia médica por su ID")]
    [EndpointDescription("Remueve permanentemente una historia médica del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteMedicalRecordCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
