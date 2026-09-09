using Api.Common.Security.Permissions;
using Api.Reports.Dtos;
using Api.Reports.Mappings;
using Application.Reports.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Reports.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("appointments-by-day")]
    [RequirePermission("Reportes", PermissionAction.View)]
    [EndpointSummary("Obtiene el volumen de citas por día calendario")]
    [EndpointDescription(
        "Retorna la cantidad de citas agrupadas por día calendario (America/Bogota) dentro del " +
        "intervalo [from, to], ambos inclusivos, en formato yyyy-MM-dd. Incluye todos los días del " +
        "intervalo aunque no tengan citas (contadores en cero). Cada día reporta totalAppointments " +
        "y su desglose por estado: attendedCount (ATENDIDA), canceledCount (CANCELADA/NO_ASISTIO) " +
        "y scheduledCount (AGENDADA/CONFIRMADA/EN_PROGRESO). Los resultados se ordenan " +
        "ascendentemente por fecha.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentsByDayReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentsByDayReportResponse>>> GetAppointmentsByDay(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetAppointmentsByDayReportQuery(from, to),
            cancellationToken);

        return Ok(result.ToResponse());
    }
}
