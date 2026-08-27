using Application.Common.Abstractions;
using MediatR;

namespace Application.AccountStatements.UseCases;

public sealed class DeleteAccountStatementCommandHandler
    : IRequestHandler<DeleteAccountStatementCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteAccountStatementCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        DeleteAccountStatementCommand request,
        CancellationToken cancellationToken)
    {
        var statement = await _uow.AccountStatementsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (statement is null)
        {
            return false;
        }

        await _uow.AccountStatementsRepository.DeleteAsync(
            statement,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
