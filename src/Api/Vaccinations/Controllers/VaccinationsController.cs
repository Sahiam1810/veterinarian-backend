using Api.Common.Security;
using Api.Vaccinations.Dtos;
using Api.Vaccinations.Mappings;
using Application.Vaccinations.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Vaccinations.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VaccinationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOrVeterinarian)]
    [EndpointSummary("Registra una nueva vacunación")]
    [EndpointDescription("Registra una vacuna aplicada a la mascota de un cliente.")]
    [ProducesResponseType(typeof(CreateVaccinationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateVaccinationResponse>> Create(
        [FromBody] CreateVaccinationRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateVaccinationResponse(id));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ClinicalHistoryReadOnly)]
    [EndpointSummary("Obtiene todas las vacunaciones")]
    [EndpointDescription("Retorna el listado completo de todas las vacunas registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<VaccinationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<VaccinationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var vaccinations = await sender.Send(
            new GetAllVaccinationsQuery(),
            cancellationToken);

        return Ok(vaccinations.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ClinicalHistoryReadOnly)]
    [EndpointSummary("Obtiene un registro de vacunación por su ID")]
    [EndpointDescription("Retorna la información detallada de un registro de vacunación específico.")]
    [ProducesResponseType(typeof(VaccinationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vaccination = await sender.Send(
            new GetVaccinationByIdQuery(id),
            cancellationToken);

        return vaccination is null
            ? NotFound()
            : Ok(vaccination.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [EndpointSummary("Actualiza un registro de vacunación existente")]
    [EndpointDescription("Modifica los datos de una vacunación previamente registrada. Solo el administrador puede corregir un registro ya creado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVaccinationRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return updated
            ? NoContent()
            : NotFound();
    }
}
