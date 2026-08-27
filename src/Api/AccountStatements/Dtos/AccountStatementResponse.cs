namespace Api.AccountStatements.Dtos;

public sealed record AccountStatementResponse(
    Guid Id,
    Guid AccountId,
    DateTime IssueDate,
    string Status,
    DateTime CreatedAt);
