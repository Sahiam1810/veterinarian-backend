using Api.Common.Security;
using Api.Common.Security.Permissions;
using Application.HospitalizationSettings.Dtos;
using Application.HospitalizationSettings.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.HospitalizationSettings.Controllers;

[ApiController]
[Route("api/hospitalization-settings")]
public sealed class HospitalizationSettingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Hospitalización", PermissionAction.View)]
    [ProducesResponseType(typeof(HospitalizationSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HospitalizationSettingsDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetHospitalizationSettingsQuery(), cancellationToken));

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [RequirePermission("Hospitalización", PermissionAction.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateHospitalizationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateHospitalizationSettingsCommand(request.DailyRate),
            cancellationToken);

        return NoContent();
    }
}
