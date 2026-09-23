using System.Security.Claims;
using Api.Common.Security.Permissions;
using Api.SupplyConsumptions.Dtos;
using Api.SupplyConsumptions.Mappings;
using Application.SupplyConsumptions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.SupplyConsumptions.Controllers;

[ApiController]
[Route("api/hospitalization-stays/{stayId:guid}/supply-consumptions")]
public sealed class SupplyConsumptionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Insumos", PermissionAction.Create)]
    [EndpointSummary("Registra un consumo de insumo para una estancia de hospitalización")]
    [ProducesResponseType(typeof(SupplyConsumptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplyConsumptionDto>> Register(
        [FromRoute] Guid stayId,
        [FromBody] CreateSupplyConsumptionDto dto,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var registeredByUserId))
        {
            return Unauthorized();
        }

        var command = dto.ToCommand(stayId, registeredByUserId);
        var consumption = await sender.Send(command, cancellationToken);
        var response = consumption.ToResponse();

        return CreatedAtAction(
            nameof(GetByStayId),
            new { stayId = response.HospitalizationStayId },
            response);
    }

    [HttpGet]
    [RequirePermission("Insumos", PermissionAction.View)]
    [EndpointSummary("Obtiene los consumos de insumos asociados a una estancia de hospitalización")]
    [ProducesResponseType(typeof(IEnumerable<SupplyConsumptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SupplyConsumptionDto>>> GetByStayId(
        [FromRoute] Guid stayId,
        CancellationToken cancellationToken = default)
    {
        var consumptions = await sender.Send(new GetSupplyConsumptionsByStayIdQuery(stayId), cancellationToken);
        return Ok(consumptions.ToResponse());
    }

    [HttpGet("total")]
    [RequirePermission("Insumos", PermissionAction.View)]
    [EndpointSummary("Obtiene el costo total acumulado por insumos para una estancia de hospitalización")]
    [ProducesResponseType(typeof(SupplyConsumptionTotalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplyConsumptionTotalDto>> GetTotalByStayId(
        [FromRoute] Guid stayId,
        CancellationToken cancellationToken = default)
    {
        var total = await sender.Send(new GetSupplyConsumptionTotalByStayIdQuery(stayId), cancellationToken);
        return Ok(new SupplyConsumptionTotalDto(stayId, total));
    }
}
