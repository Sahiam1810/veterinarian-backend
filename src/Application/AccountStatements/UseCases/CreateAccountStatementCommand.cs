using MediatR;

namespace Application.AccountStatements.UseCases;

public sealed record CreateAccountStatementCommand(
    Guid AccountId,
    DateTime IssueDate,
    string Status) : IRequest<Guid>;
