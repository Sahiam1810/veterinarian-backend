using System.Security.Claims;
using Api.Common.Security;
using Api.Common.Security.Permissions;
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
    [RequirePermission("Historiales Clínicos", PermissionAction.Create)]
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
    [Authorize(Policy = AuthorizationPolicies.ClinicalStaffOnly)]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene todas las vacunaciones")]
    [EndpointDescription("Retorna el listado completo de vacunas para el personal clínico autorizado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<VaccinationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<VaccinationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var vaccinations = await sender.Send(
            new GetAllVaccinationsQuery(),
            cancellationToken);

        return Ok(vaccinations.ToResponse());
    }

    [HttpGet("mine")]
    [Authorize(Policy = AuthorizationPolicies.ClientOnly)]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene las vacunaciones de las mascotas del cliente")]
    [EndpointDescription("Deriva la cuenta desde el JWT y retorna únicamente las vacunas de las mascotas del cliente autenticado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<VaccinationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<VaccinationResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(out var userAccountId))
        {
            return Unauthorized();
        }

        var vaccinations = await sender.Send(
            new GetMyVaccinationsQuery(userAccountId),
            cancellationToken);

        return Ok(vaccinations.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ClinicalStaffOnly)]
    [RequirePermission("Historiales Clínicos", PermissionAction.View)]
    [EndpointSummary("Obtiene un registro de vacunación por su ID")]
    [EndpointDescription("Retorna un registro de vacunación específico para el personal clínico autorizado.")]
    [ProducesResponseType(typeof(VaccinationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vaccination = await sender.Send(
            new GetVaccinationByIdQuery(id),
            cancellationToken);

        return Ok(vaccination.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Historiales Clínicos", PermissionAction.Edit)]
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
        TryGetUserAccountId(out var userAccountId);

        await sender.Send(
            request.ToCommand(id, userAccountId),
            cancellationToken);

        return NoContent();
    }

    private bool TryGetUserAccountId(out Guid userAccountId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out userAccountId);
    }
}
