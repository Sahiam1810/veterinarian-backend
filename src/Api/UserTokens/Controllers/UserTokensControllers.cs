using Api.Common.Security;
using Api.UserTokens.Dtos;
using Api.UserTokens.Mappings;
using Application.UserTokens.UseCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.UserTokens.Controllers;

// SEC-04: manipular tokens de sesión (crearlos a mano, verlos, borrarlos de
// cualquier cuenta) es tan sensible como resetear la contraseña de otro
// (SEC-02) -- mismo criterio, exclusivo de SuperAdmin. La creación manual,
// además, rechaza los tipos "refresh"/"access" en el validator: esos solo
// pueden originarse del flujo real de login/refresh.
[ApiController]
[Route("api/[controller]")]
public sealed class UserTokensController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Registra un nuevo token de sesión")]
    [EndpointDescription("Exclusivo de SuperAdmin. Registra un token no autenticante (ej. reset_password) asociado a una cuenta, con su fecha de expiración. No permite crear tokens de tipo 'refresh' ni 'access': esos solo pueden originarse del flujo real de login/refresh.")]
    [ProducesResponseType(typeof(CreateUserTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateUserTokenResponse>> Create(
        [FromBody] CreateUserTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokenId = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateUserTokenResponse(tokenId));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene un token por su ID")]
    [EndpointDescription("Exclusivo de SuperAdmin. Retorna la información de un token específico por su identificador GUID.")]
    [ProducesResponseType(typeof(UserTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserTokenResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var token = await sender.Send(
            new GetUserTokenByIdQuery(id),
            cancellationToken);

        return Ok(token.ToResponse());
    }

    [HttpGet("by-account/{accountId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Obtiene los tokens de una cuenta")]
    [EndpointDescription("Exclusivo de SuperAdmin. Retorna todos los tokens activos e inactivos asociados a una cuenta de usuario.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserTokenResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserTokenResponse>>> GetByAccountId(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(
            new GetUserTokensByAccountIdQuery(accountId),
            cancellationToken);

        return Ok(tokens.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [EndpointSummary("Revoca un token")]
    [EndpointDescription("Exclusivo de SuperAdmin. Elimina un token (por ejemplo, para cerrar sesión o invalidar un refresh token).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteUserTokenCommand(id),
            cancellationToken);

        return NoContent();
    }
}
