using Api.UserTokens.Dtos;
using Api.UserTokens.Mappings;
using Application.UserTokens.UseCase;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.UserTokens.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UserTokensController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registra un nuevo token de sesión")]
    [EndpointDescription("Registra un token (refresh, reset_password, etc.) asociado a una cuenta, con su fecha de expiración.")]
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
    [EndpointSummary("Obtiene un token por su ID")]
    [EndpointDescription("Retorna la información de un token específico por su identificador GUID.")]
    [ProducesResponseType(typeof(UserTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserTokenResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var token = await sender.Send(
            new GetUserTokenByIdQuery(id),
            cancellationToken);

        return token is null
            ? NotFound()
            : Ok(token.ToResponse());
    }

    [HttpGet("by-account/{accountId:guid}")]
    [EndpointSummary("Obtiene los tokens de una cuenta")]
    [EndpointDescription("Retorna todos los tokens activos e inactivos asociados a una cuenta de usuario.")]
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
    [EndpointSummary("Revoca un token")]
    [EndpointDescription("Elimina un token (por ejemplo, para cerrar sesión o invalidar un refresh token).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteUserTokenCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
