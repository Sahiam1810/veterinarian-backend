using MediatR;

namespace Application.AccountStatements.UseCases;

public sealed record UpdateAccountStatementStatusCommand(
    Guid Id,
    string Status) : IRequest<bool>;
