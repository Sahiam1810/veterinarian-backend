using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Veterinarians.Dtos;
using Api.Veterinarians.Mappings;
using Application.Veterinarians.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Veterinarians.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VeterinariansController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Veterinarios", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo veterinario")]
    [EndpointDescription("Registra un nuevo veterinario asociando su usuario, especialidad y tarjeta profesional.")]
    [ProducesResponseType(typeof(CreateVeterinarianResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateVeterinarianResponse>> Create(
        [FromBody] CreateVeterinarianRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateVeterinarianResponse(id));
    }

    [HttpGet]
    [RequirePermission("Veterinarios", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los veterinarios")]
    [EndpointDescription("Retorna el listado completo de todos los veterinarios registrados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<VeterinarianResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<VeterinarianResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var veterinarians = await sender.Send(
            new GetAllVeterinariansQuery(),
            cancellationToken);

        return Ok(veterinarians.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Veterinarios", PermissionAction.View)]
    [EndpointSummary("Obtiene un veterinario por su ID")]
    [EndpointDescription("Retorna la información detallada de un veterinario específico.")]
    [ProducesResponseType(typeof(VeterinarianResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeterinarianResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var veterinarian = await sender.Send(
            new GetVeterinarianByIdQuery(id),
            cancellationToken);

        return Ok(veterinarian.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Veterinarios", PermissionAction.Edit)]
    [EndpointSummary("Actualiza un veterinario existente")]
    [EndpointDescription("Modifica la información de un veterinario previamente registrado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVeterinarianRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Veterinarios", PermissionAction.Delete)]
    [EndpointSummary("Elimina un veterinario por su ID")]
    [EndpointDescription("Remueve permanentemente un veterinario del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteVeterinarianCommand(id),
            cancellationToken);

        return NoContent();
    }
}
