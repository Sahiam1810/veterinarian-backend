using Api.Availabilities.Dtos;
using Api.Availabilities.Mappings;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Application.Availabilities.UseCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Availabilities.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AvailabilitiesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Citas", PermissionAction.Create)]
    [EndpointSummary("Crea una nueva disponibilidad")]
    [EndpointDescription("Registra una franja horaria recurrente (día de la semana, hora de inicio y fin) en la que un veterinario atiende.")]
    [ProducesResponseType(typeof(CreateAvailabilityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAvailabilityResponse>> Create(
        [FromBody] CreateAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateAvailabilityResponse(id));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene todas las disponibilidades")]
    [EndpointDescription("Retorna el listado completo de franjas de disponibilidad de todos los veterinarios.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvailabilityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AvailabilityResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var availabilities = await sender.Send(
            new GetAllAvailabilitiesQuery(),
            cancellationToken);

        return Ok(availabilities.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene una disponibilidad por su ID")]
    [EndpointDescription("Retorna la información detallada de una franja de disponibilidad específica.")]
    [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var availability = await sender.Send(
            new GetAvailabilityByIdQuery(id),
            cancellationToken);

        return availability is null
            ? NotFound()
            : Ok(availability.ToResponse());
    }

    [HttpGet("by-veterinarian/{veterinarianId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene las disponibilidades de un veterinario")]
    [EndpointDescription("Retorna todas las franjas de disponibilidad configuradas para un veterinario, ordenadas por día y hora.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvailabilityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AvailabilityResponse>>> GetByVeterinarianId(
        Guid veterinarianId,
        CancellationToken cancellationToken)
    {
        var availabilities = await sender.Send(
            new GetAvailabilitiesByVeterinarianIdQuery(veterinarianId),
            cancellationToken);

        return Ok(availabilities.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Citas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una disponibilidad existente")]
    [EndpointDescription("Modifica el día, horario o estado de una franja de disponibilidad previamente registrada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAvailabilityRequest request,
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
    [RequirePermission("Citas", PermissionAction.Delete)]
    [EndpointSummary("Elimina una disponibilidad por su ID")]
    [EndpointDescription("Remueve permanentemente una franja de disponibilidad del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteAvailabilityCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
