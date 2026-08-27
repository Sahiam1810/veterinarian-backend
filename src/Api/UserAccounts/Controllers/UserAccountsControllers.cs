using Api.Common.Security;
using Api.UserAccounts.Dtos;
using Api.UserAccounts.Mappings;
using Application.UserAccounts.UseCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.UserAccounts.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class UserAccountsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Crea una nueva cuenta de usuario")]
    [EndpointDescription("Registra las credenciales de acceso (usuario, correo y estado) asociadas a un usuario existente.")]
    [ProducesResponseType(typeof(CreateUserAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserAccountResponse>> Create(
        [FromBody] CreateUserAccountRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateUserAccountResponse(accountId));
    }

    [HttpGet]
    [EndpointSummary("Obtiene todas las cuentas de usuario")]
    [EndpointDescription("Retorna el listado de todas las cuentas de acceso registradas en la plataforma.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserAccountResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserAccountResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var accounts = await sender.Send(
            new GetAllUserAccountsQuery(),
            cancellationToken);

        return Ok(accounts.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene una cuenta de usuario por su ID")]
    [EndpointDescription("Retorna la información de una cuenta de usuario específica por su identificador GUID.")]
    [ProducesResponseType(typeof(UserAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserAccountResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await sender.Send(
            new GetUserAccountByIdQuery(id),
            cancellationToken);

        return account is null
            ? NotFound()
            : Ok(account.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza una cuenta de usuario")]
    [EndpointDescription("Modifica el nombre de usuario, correo o estado de una cuenta existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserAccountRequest request,
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
    [EndpointSummary("Elimina una cuenta de usuario")]
    [EndpointDescription("Remueve la cuenta de acceso de un usuario del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteUserAccountCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
