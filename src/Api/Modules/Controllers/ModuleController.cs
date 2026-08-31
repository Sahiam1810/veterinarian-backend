using Api.Common.Security;
using Api.Modules.Dtos;
using Api.Modules.Mappings;
using Application.Modules.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ModuleController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene los módulos")]
    [EndpointDescription("Lista todos los módulos registrados ordenados por nombre.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ModuleResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ModuleResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var modules = await sender.Send(new GetModulesQuery(), cancellationToken);
        return Ok(modules.Select(module => module.ToDto()).ToArray());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene un módulo")]
    [EndpointDescription("Busca un módulo por su identificador.")]
    [ProducesResponseType(typeof(ModuleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var module = await sender.Send(new GetModuleByIdQuery(id), cancellationToken);
        return Ok(module.ToDto());
    }

    [HttpPost]
    [EndpointSummary("Crea un módulo")]
    [EndpointDescription("Registra un nuevo módulo.")]
    [ProducesResponseType(typeof(ModuleResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ModuleResponseDto>> Create(
        [FromBody] CreateModuleDto dto,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new CreateModuleCommand(dto.Name, dto.Description),
            cancellationToken);
        var module = await sender.Send(new GetModuleByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, module.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza un módulo")]
    [EndpointDescription("Actualiza el nombre o la descripción de un módulo existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateModuleDto dto,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateModuleCommand(id, dto.Name, dto.Description),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina un módulo")]
    [EndpointDescription("Elimina físicamente un módulo por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteModuleCommand(id), cancellationToken);
        return NoContent();
    }
}
