using Api.Common.Security.Permissions;
using Api.Reports.Dtos;
using Api.Reports.Mappings;
using Application.Reports.UseCases;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Reports.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("appointments-by-veterinarian")]
    [RequirePermission("Reportes", PermissionAction.View)]
    [EndpointSummary("Reporte de citas por veterinario")]
    [EndpointDescription("Agrupa las citas cuya fecha programada está entre from y to (inclusive), en formato yyyy-MM-dd. AGENDADA, CONFIRMADA y EN_PROGRESO cuentan como vigentes; ATENDIDA y CANCELADA se cuentan por separado y los demás estados como other.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentVeterinarianReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentVeterinarianReportResponse>>> GetAppointmentsByVeterinarian(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var report = await sender.Send(
            new GetAppointmentsByVeterinarianReportQuery(from, to),
            cancellationToken);

        return Ok(report.Select(x => new AppointmentVeterinarianReportResponse(
            x.VeterinarianId,
            x.VeterinarianName,
            x.TotalAppointments,
            x.AttendedCount,
            x.CanceledCount,
            x.ScheduledCount,
            x.OtherCount)));
    }

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

    [HttpGet("appointments-by-status")]
    [RequirePermission("Reportes", PermissionAction.View)]
    [EndpointSummary("Reporte de citas por estado")]
    [EndpointDescription("Obtiene la distribución por estado de las citas dentro del período especificado, incluyendo conteos y porcentajes.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentStatusReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<AppointmentStatusReportResponseDto>>> GetAppointmentsByStatus(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var query = new GetAppointmentsByStatusReportQuery(from, to);
        var result = await sender.Send(query, cancellationToken);

        var response = result
            .Select(x => new AppointmentStatusReportResponseDto(
                x.StatusId,
                x.StatusName,
                x.Count,
                x.Percentage))
            .ToList();

        return Ok(response);
    }
}
