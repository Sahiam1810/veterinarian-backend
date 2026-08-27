using Api.Common.Security;
using Api.ProviderModelsAi.Dtos;
using Api.ProviderModelsAi.Mappings;
using Application.Common.Exceptions;
using Application.ProviderModelsAi.UseCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.ProviderModelsAi.Controllers;

[ApiController]
[Route("api/ai/providers")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class ProviderModelAiController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un proveedor de inteligencia artificial")]
    [EndpointDescription("Registra un nuevo proveedor de IA. Las fechas de auditoría las asigna el dominio.")]
    [ProducesResponseType(typeof(ProviderModelAiResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProviderModelAiResponseDto>> Create(
        [FromBody] CreateProviderModelAiDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = provider.ToResponse();
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener un proveedor de IA por su identificador")]
    [EndpointDescription("Devuelve la configuración de un proveedor de inteligencia artificial.")]
    [ProducesResponseType(typeof(ProviderModelAiResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderModelAiResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var provider = await sender.Send(new GetProviderModelAiByIdQuery(id), cancellationToken);
        return provider is null
            ? NotFound(new { Message = $"No se encontró el proveedor de IA '{id}'." })
            : Ok(provider.ToResponse());
    }

    [HttpGet]
    [EndpointSummary("Listar proveedores de IA")]
    [EndpointDescription("Devuelve todos los proveedores de inteligencia artificial configurados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProviderModelAiResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProviderModelAiResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var providers = await sender.Send(new GetAllProviderModelAisQuery(), cancellationToken);
        return Ok(providers.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar un proveedor de IA")]
    [EndpointDescription("Actualiza el nombre, la razón social y el sitio web del proveedor. No modifica el estado de activación.")]
    [ProducesResponseType(typeof(ProviderModelAiResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderModelAiResponseDto>> Update(
        Guid id,
        [FromBody] UpdateProviderModelAiDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(provider.ToResponse());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/activate")]
    [EndpointSummary("Activar un proveedor de IA")]
    [EndpointDescription("Marca el proveedor como activo para que sus modelos puedan usarse en la configuración.")]
    [ProducesResponseType(typeof(ProviderModelAiResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderModelAiResponseDto>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await sender.Send(new ActivateProviderModelAiCommand(id), cancellationToken);
            return Ok(provider.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    [EndpointSummary("Desactivar un proveedor de IA")]
    [EndpointDescription("Marca el proveedor como inactivo.")]
    [ProducesResponseType(typeof(ProviderModelAiResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderModelAiResponseDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await sender.Send(new DeactivateProviderModelAiCommand(id), cancellationToken);
            return Ok(provider.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
