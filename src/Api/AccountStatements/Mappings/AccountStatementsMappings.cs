using Api.AccountStatements.Dtos;
using Application.AccountStatements.UseCases;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Api.AccountStatements.Mappings;

public static class AccountStatementsMappings
{
    public static CreateAccountStatementCommand ToCommand(
        this CreateAccountStatementRequest request)
    {
        return new CreateAccountStatementCommand(
            request.AccountId,
            request.IssueDate,
            request.Status);
    }

    public static UpdateAccountStatementStatusCommand ToCommand(
        this UpdateAccountStatementStatusRequest request,
        Guid id)
    {
        return new UpdateAccountStatementStatusCommand(
            id,
            request.Status);
    }

    public static AccountStatementResponse ToResponse(this AccountStatementEntity statement)
    {
        return new AccountStatementResponse(
            statement.Id,
            statement.AccountId,
            statement.IssueDate,
            statement.Status.Value,
            statement.CreatedAt);
    }

    public static IReadOnlyCollection<AccountStatementResponse> ToResponse(
        this IReadOnlyCollection<AccountStatementEntity> statements)
    {
        return statements
            .Select(statement => statement.ToResponse())
            .ToArray();
    }
}
