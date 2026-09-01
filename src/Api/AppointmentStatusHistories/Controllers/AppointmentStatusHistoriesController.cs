using Api.AppointmentStatusHistories.Dtos;
using Api.AppointmentStatusHistories.Mappings;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Application.AppointmentStatusHistories.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.AppointmentStatusHistories.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppointmentStatusHistoriesController(ISender sender) : ControllerBase
{
    // Cambiar el estado de una cita (ej. marcarla como atendida) se modela como
    // "Editar" sobre el módulo Citas, no como "Crear" — así el Veterinario (que
    // solo tiene Ver+Editar en Citas, sin Crear) puede marcar la atención.
    [HttpPost]
    [RequirePermission("Citas", PermissionAction.Edit)]
    [EndpointSummary("Crea un nuevo historial de estado de cita")]
    [EndpointDescription("Registra un nuevo historial de cambio de estado para una cita médica.")]
    [ProducesResponseType(typeof(CreateAppointmentStatusHistoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAppointmentStatusHistoryResponse>> Create(
        [FromBody] CreateAppointmentStatusHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateAppointmentStatusHistoryResponse(id));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene todos los historiales de estado de citas")]
    [EndpointDescription("Retorna el listado completo de los historiales de estado de citas registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentStatusHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentStatusHistoryResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var histories = await sender.Send(
            new GetAllAppointmentStatusHistoriesQuery(),
            cancellationToken);

        return Ok(histories.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene un historial de estado de cita por su ID")]
    [EndpointDescription("Retorna la información detallada de un historial de estado específico.")]
    [ProducesResponseType(typeof(AppointmentStatusHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentStatusHistoryResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await sender.Send(
            new GetAppointmentStatusHistoryByIdQuery(id),
            cancellationToken);

        return Ok(history.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Citas", PermissionAction.Edit)]
    [EndpointSummary("Actualiza un historial de estado de cita existente")]
    [EndpointDescription("Modifica los datos de un historial de estado previamente registrado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAppointmentStatusHistoryRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Citas", PermissionAction.Delete)]
    [EndpointSummary("Elimina un historial de estado de cita por su ID")]
    [EndpointDescription("Remueve permanentemente un historial de estado del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteAppointmentStatusHistoryCommand(id),
            cancellationToken);

        return NoContent();
    }
}
