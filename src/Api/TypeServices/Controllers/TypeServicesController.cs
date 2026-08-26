using Api.TypeServices.Dtos;
using Api.TypeServices.Mappings;
using Application.TypeServices.UseCases;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.TypeServices.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TypeServicesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crea un nuevo tipo de servicio")]
    [EndpointDescription("Registra un nuevo tipo de servicio en el sistema.")]
    [ProducesResponseType(typeof(CreateTypeServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTypeServiceResponse>> Create(
        [FromBody] CreateTypeServiceRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateTypeServiceResponse(id));
    }

    [HttpGet]
    [EndpointSummary("Obtiene todos los tipos de servicios")]
    [EndpointDescription("Retorna el listado completo de todos los tipos de servicios configurados en el sistema.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TypeServiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TypeServiceResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var services = await sender.Send(
            new GetAllTypeServicesQuery(),
            cancellationToken);

        return Ok(services.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un tipo de servicio por su ID")]
    [EndpointDescription("Retorna la información detallada de un tipo de servicio específico.")]
    [ProducesResponseType(typeof(TypeServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TypeServiceResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await sender.Send(
            new GetTypeServiceByIdQuery(id),
            cancellationToken);

        return service is null
            ? NotFound()
            : Ok(service.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un tipo de servicio existente")]
    [EndpointDescription("Modifica el nombre y/o descripción de un tipo de servicio previamente registrado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTypeServiceRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un tipo de servicio por su ID")]
    [EndpointDescription("Remueve permanentemente un tipo de servicio del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteTypeServiceCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
