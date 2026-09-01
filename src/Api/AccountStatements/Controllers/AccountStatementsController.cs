using Api.AccountStatements.Dtos;
using Api.AccountStatements.Mappings;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Application.AccountStatements.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.AccountStatements.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AccountStatementsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Cuentas y Pagos", PermissionAction.Create)]
    [EndpointSummary("Registra un nuevo estado de cuenta")]
    [EndpointDescription("Genera un estado de cuenta asociado a una cuenta de usuario, con su fecha de emisión y estado inicial.")]
    [ProducesResponseType(typeof(CreateAccountStatementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateAccountStatementResponse>> Create(
        [FromBody] CreateAccountStatementRequest request,
        CancellationToken cancellationToken)
    {
        var statementId = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateAccountStatementResponse(statementId));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene un estado de cuenta por su ID")]
    [EndpointDescription("Retorna la información de un estado de cuenta específico por su identificador GUID.")]
    [ProducesResponseType(typeof(AccountStatementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountStatementResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var statement = await sender.Send(
            new GetAccountStatementByIdQuery(id),
            cancellationToken);

        return Ok(statement.ToResponse());
    }

    [HttpGet("by-account/{accountId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffOnly)]
    [EndpointSummary("Obtiene los estados de cuenta de una cuenta")]
    [EndpointDescription("Retorna todos los estados de cuenta asociados a una cuenta de usuario, ordenados por fecha de emisión descendente.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AccountStatementResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AccountStatementResponse>>> GetByAccountId(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var statements = await sender.Send(
            new GetAccountStatementsByAccountIdQuery(accountId),
            cancellationToken);

        return Ok(statements.ToResponse());
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission("Cuentas y Pagos", PermissionAction.Edit)]
    [EndpointSummary("Actualiza el estado de un estado de cuenta")]
    [EndpointDescription("Cambia el estado (por ejemplo, de pendiente a pagado) de un estado de cuenta existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateAccountStatementStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Cuentas y Pagos", PermissionAction.Delete)]
    [EndpointSummary("Elimina un estado de cuenta")]
    [EndpointDescription("Elimina de forma permanente un estado de cuenta existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteAccountStatementCommand(id),
            cancellationToken);

        return NoContent();
    }
}
