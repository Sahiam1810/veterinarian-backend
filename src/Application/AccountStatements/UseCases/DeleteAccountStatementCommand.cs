using MediatR;

namespace Application.AccountStatements.UseCases;

public sealed record DeleteAccountStatementCommand(Guid Id) : IRequest<bool>;
