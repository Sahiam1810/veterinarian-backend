using Application.Common.Abstractions;
using MediatR;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Application.AccountStatements.UseCases;

public sealed class CreateAccountStatementCommandHandler
    : IRequestHandler<CreateAccountStatementCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateAccountStatementCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateAccountStatementCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.AccountId,
            cancellationToken);

        if (account is null)
        {
            throw new KeyNotFoundException(
                "La cuenta especificada no existe.");
        }

        var statement = new AccountStatementEntity(
            request.AccountId,
            request.IssueDate,
            request.Status);

        await _uow.AccountStatementsRepository.AddAsync(
            statement,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return statement.Id;
    }
}
