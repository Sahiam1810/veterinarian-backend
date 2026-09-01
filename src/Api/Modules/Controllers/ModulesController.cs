using Api.Common.Security;
using Api.Modules.Dtos;
using Api.Modules.Mappings;
using Application.Modules.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ModulesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene los módulos")]
    [EndpointDescription("Lista los módulos registrados para permisos por rol.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ModuleResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ModuleResponseDto>>> GetAll(CancellationToken ct) =>
        Ok((await sender.Send(new GetAllModulesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene un módulo")]
    [EndpointDescription("Busca un módulo por su identificador.")]
    [ProducesResponseType(typeof(ModuleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleResponseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok((await sender.Send(new GetModuleByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Crea un módulo")]
    [EndpointDescription("Registra un nuevo módulo del sistema.")]
    [ProducesResponseType(typeof(ModuleResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ModuleResponseDto>> Create(CreateModuleDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateModuleCommand(dto.Name, dto.Description), ct);
        var module = await sender.Send(new GetModuleByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, module.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Actualiza un módulo")]
    [EndpointDescription("Actualiza el nombre o la descripción de un módulo.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateModuleDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateModuleCommand(id, dto.Name, dto.Description), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Elimina un módulo")]
    [EndpointDescription("Elimina un módulo por su identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteModuleCommand(id), ct);
        return NoContent();
    }
}
