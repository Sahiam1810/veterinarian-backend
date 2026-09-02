using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Services.Dtos;
using Api.Services.Mappings;
using Application.Services.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Services.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ServicesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Servicios", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo servicio")]
    [EndpointDescription("Registra un nuevo servicio veterinario en el sistema.")]
    [ProducesResponseType(typeof(CreateServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateServiceResponse>> Create(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateServiceResponse(id));
    }

    [HttpGet]
    [RequirePermission("Servicios", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los servicios")]
    [EndpointDescription("Retorna el listado completo de todos los servicios registrados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var services = await sender.Send(
            new GetAllServicesQuery(),
            cancellationToken);

        return Ok(services.ToResponse());
    }

    [HttpGet("available")]
    [Authorize]
    [EndpointSummary("Obtiene el catálogo público de servicios activos")]
    [EndpointDescription("Retorna únicamente servicios veterinarios activos para consumidores autenticados, incluido el canal invitado de Telegram.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceResponse>>> GetAvailable(
        CancellationToken cancellationToken)
    {
        var services = await sender.Send(
            new GetAvailableServicesQuery(),
            cancellationToken);

        return Ok(services.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Servicios", PermissionAction.View)]
    [EndpointSummary("Obtiene un servicio por su ID")]
    [EndpointDescription("Retorna la información detallada de un servicio específico.")]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await sender.Send(
            new GetServiceByIdQuery(id),
            cancellationToken);

        return Ok(service.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Servicios", PermissionAction.Edit)]
    [EndpointSummary("Actualiza un servicio existente")]
    [EndpointDescription("Modifica los datos de un servicio previamente registrado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Servicios", PermissionAction.Delete)]
    [EndpointSummary("Elimina un servicio por su ID")]
    [EndpointDescription("Remueve permanentemente un servicio del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteServiceCommand(id),
            cancellationToken);

        return NoContent();
    }
}
