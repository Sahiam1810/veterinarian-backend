using Api.Common.Security;
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
