using System.Security.Claims;
using Api.Clients.Dtos;
using Api.Clients.Mappings;
using Api.Common.Security;
using Application.Clients.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    // GET /api/clients
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
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
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
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
    [Authorize(Policy = AuthorizationPolicies.FrontDeskStaffOnly)]
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
            dto.RegistrationDate), ct);

        var client = await sender.Send(new GetClientByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, client.ToDto());
    }

    // PUT /api/clients/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FrontDeskStaffOnly)]
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
            dto.RegistrationDate), ct);

        return NoContent();
    }

    // DELETE /api/clients/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
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
