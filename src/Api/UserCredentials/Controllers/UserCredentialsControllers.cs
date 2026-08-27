using Api.Common.Security;
using Api.UserCredentials.Dtos;
using Api.UserCredentials.Mappings;
using Application.UserCredentials.UseCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.UserCredentials.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class UserCredentialsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crea las credenciales de una cuenta")]
    [EndpointDescription("Registra la contraseña inicial (hasheada) de una cuenta de usuario existente. El hash nunca se expone en la respuesta.")]
    [ProducesResponseType(typeof(CreateUserCredentialsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserCredentialsResponse>> Create(
        [FromBody] CreateUserCredentialsRequest request,
        CancellationToken cancellationToken)
    {
        var credentialsId = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateUserCredentialsResponse(credentialsId));
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene los metadatos de unas credenciales")]
    [EndpointDescription("Retorna la información no sensible de las credenciales (cuenta asociada y fecha del último cambio). Nunca retorna el hash de la contraseña.")]
    [ProducesResponseType(typeof(UserCredentialsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCredentialsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var credentials = await sender.Send(
            new GetUserCredentialsByIdQuery(id),
            cancellationToken);

        return credentials is null
            ? NotFound()
            : Ok(credentials.ToResponse());
    }

    [HttpGet("by-account/{accountId:guid}")]
    [EndpointSummary("Obtiene los metadatos de las credenciales de una cuenta")]
    [EndpointDescription("Retorna la información no sensible de las credenciales asociadas a una cuenta de usuario.")]
    [ProducesResponseType(typeof(UserCredentialsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCredentialsResponse>> GetByAccountId(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var credentials = await sender.Send(
            new GetUserCredentialsByAccountIdQuery(accountId),
            cancellationToken);

        return credentials is null
            ? NotFound()
            : Ok(credentials.ToResponse());
    }

    [HttpPatch("{id:guid}/change-password")]
    [EndpointSummary("Cambia la contraseña de una cuenta")]
    [EndpointDescription("Valida la contraseña actual y, si es correcta, la reemplaza por la nueva contraseña (hasheada).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        Guid id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var changed = await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return changed
            ? NoContent()
            : NotFound();
    }
}
