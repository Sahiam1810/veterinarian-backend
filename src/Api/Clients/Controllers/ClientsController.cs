using System.Security.Claims;
using Api.Clients.Dtos;
using Api.Clients.Mappings;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Application.Clients.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Clients.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(ISender sender) : ControllerBase
{
    // GET /api/clients/me
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ClientOnly)]
    [EndpointSummary("Obtiene el perfil del cliente autenticado")]
    [EndpointDescription("Retorna los datos del cliente asociado al usuario autenticado actual (portal de dueño).")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponseDto>> GetMe(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var client = await sender.Send(new GetMyClientQuery(userAccountId), ct);
        return Ok(client.ToDto());
    }

    // GET /api/clients/by-identification/{identificationNumber}
    [HttpGet("by-identification/{identificationNumber}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ClientIdentificationLookup)]
    [EndpointSummary("Resuelve un cliente por número de identificación")]
    [EndpointDescription("Permite al chatbot ubicar al cliente antes de un JWT tradicional. Respuesta acotada (sin dirección ni teléfono) porque es anónimo y rate-limited.")]
    [ProducesResponseType(typeof(ClientIdentificationLookupResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ClientIdentificationLookupResponseDto>> GetByIdentification(
        string identificationNumber,
        CancellationToken ct)
    {
        var client = await sender.Send(new GetClientByIdentificationQuery(identificationNumber), ct);
        return Ok(client.ToIdentificationLookupResponse());
    }

    // GET /api/clients/lookup
    [HttpGet("lookup")]
    [RequirePermission("Clientes", PermissionAction.View)]
    [EndpointSummary("Busca un cliente por cédula y/o teléfono (Staff)")]
    [EndpointDescription("Retorna la información operativa completa de un cliente buscando por cédula, teléfono o ambos (lógica AND). Requiere al menos un parámetro.")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponseDto>> Lookup(
        [FromQuery] string? identification,
        [FromQuery] string? phone,
        CancellationToken ct)
    {
        var client = await sender.Send(new GetClientLookupQuery(identification, phone), ct);
        return Ok(client.ToDto());
    }

    // GET /api/clients
    [HttpGet]
    [RequirePermission("Clientes", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los clientes")]
    [EndpointDescription("Retorna una lista de todos los clientes registrados en el sistema.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClientResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClientResponseDto>>> GetAll(CancellationToken ct)
    {
        var clients = await sender.Send(new GetAllClientsQuery(), ct);
        return Ok(clients.Select(c => c.ToDto()).ToList());
    }

    // GET /api/clients/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("Clientes", PermissionAction.View)]
    [EndpointSummary("Obtiene un cliente por su ID")]
    [EndpointDescription("Retorna los detalles de un cliente específico buscando por su identificador único.")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var client = await sender.Send(new GetClientByIdQuery(id), ct);
        return Ok(client.ToDto());
    }

    // POST /api/clients
    [HttpPost]
    [RequirePermission("Clientes", PermissionAction.Create)]
    [EndpointSummary("Registra un nuevo cliente")]
    [EndpointDescription("Crea un nuevo registro de cliente asociándolo a un usuario existente.")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientResponseDto>> Create([FromBody] CreateClientDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateClientCommand(
            dto.UserId,
            dto.IdentificationNumber,
            dto.Address,
            dto.RegistrationDate,
            dto.PhoneNumber), ct);

        var client = await sender.Send(new GetClientByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, client.ToDto());
    }

    // PUT /api/clients/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("Clientes", PermissionAction.Edit)]
    [EndpointSummary("Actualiza los datos de un cliente")]
    [EndpointDescription("Modifica los datos de un cliente existente identificado por su ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateClientCommand(
            id,
            dto.UserId,
            dto.IdentificationNumber,
            dto.Address,
            dto.RegistrationDate,
            dto.PhoneNumber), ct);

        return NoContent();
    }

    // DELETE /api/clients/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("Clientes", PermissionAction.Delete)]
    [EndpointSummary("Elimina un cliente")]
    [EndpointDescription("Elimina permanentemente el registro de un cliente del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteClientCommand(id), ct);
        return NoContent();
    }
}
