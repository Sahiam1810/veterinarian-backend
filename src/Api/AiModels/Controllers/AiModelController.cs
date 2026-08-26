using Api.AiModels.Dtos;
using Api.AiModels.Mappings;
using Application.AiModels.UseCase;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.AiModels.Controllers;

[ApiController]
[Route("api/ai/models")]
public sealed class AiModelController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crear un modelo de inteligencia artificial")]
    [EndpointDescription("Crea un modelo asociado a un proveedor existente. Los precios por token y los límites no pueden ser negativos.")]
    [ProducesResponseType(typeof(AiModelResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiModelResponseDto>> Create(
        [FromBody] CreateAiModelDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = await sender.Send(dto.ToCommand(), cancellationToken);
            var response = model.ToResponse();
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
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

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtener un modelo de IA por su identificador")]
    [EndpointDescription("Devuelve la configuración de un modelo de inteligencia artificial.")]
    [ProducesResponseType(typeof(AiModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiModelResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var model = await sender.Send(new GetAiModelByIdQuery(id), cancellationToken);
        return model is null
            ? NotFound(new { Message = $"No se encontró el modelo de IA '{id}'." })
            : Ok(model.ToResponse());
    }

    [HttpGet]
    [EndpointSummary("Listar modelos de IA")]
    [EndpointDescription("Devuelve todos los modelos de inteligencia artificial configurados.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AiModelResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AiModelResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var models = await sender.Send(new GetAllAiModelsQuery(), cancellationToken);
        return Ok(models.ToResponse());
    }

    [HttpGet("by-provider/{providerId:guid}")]
    [EndpointSummary("Listar modelos de IA por proveedor")]
    [EndpointDescription("Devuelve los modelos de IA que pertenecen al proveedor indicado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AiModelResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AiModelResponseDto>>> GetByProviderId(
        Guid providerId,
        CancellationToken cancellationToken)
    {
        var models = await sender.Send(new GetAiModelsByProviderIdQuery(providerId), cancellationToken);
        return Ok(models.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualizar un modelo de IA")]
    [EndpointDescription("Actualiza los metadatos, precios y límites de tokens del modelo. No modifica el estado de activación.")]
    [ProducesResponseType(typeof(AiModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiModelResponseDto>> Update(
        Guid id,
        [FromBody] UpdateAiModelDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = await sender.Send(dto.ToCommand(id), cancellationToken);
            return Ok(model.ToResponse());
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
    [EndpointSummary("Activar un modelo de IA")]
    [EndpointDescription("Marca el modelo como activo para que pueda asignarse como predeterminado en una conversación.")]
    [ProducesResponseType(typeof(AiModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiModelResponseDto>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = await sender.Send(new ActivateAiModelCommand(id), cancellationToken);
            return Ok(model.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    [EndpointSummary("Desactivar un modelo de IA")]
    [EndpointDescription("Marca el modelo como inactivo.")]
    [ProducesResponseType(typeof(AiModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiModelResponseDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = await sender.Send(new DeactivateAiModelCommand(id), cancellationToken);
            return Ok(model.ToResponse());
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
}
