namespace Api.AccountStatements.Dtos;

public sealed record CreateAccountStatementRequest(
    Guid AccountId,
    DateTime IssueDate,
    string Status);
