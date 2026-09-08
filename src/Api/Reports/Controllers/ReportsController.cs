using Api.Common.Security.Permissions;
using Api.Reports.Dtos;
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
}
