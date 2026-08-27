using Application.Common.Abstractions;
using MediatR;

namespace Application.AccountStatements.UseCases;

public sealed class UpdateAccountStatementStatusCommandHandler
    : IRequestHandler<UpdateAccountStatementStatusCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateAccountStatementStatusCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        UpdateAccountStatementStatusCommand request,
        CancellationToken cancellationToken)
    {
        var statement = await _uow.AccountStatementsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (statement is null)
        {
            return false;
        }

        statement.UpdateStatus(request.Status);

        await _uow.AccountStatementsRepository.UpdateAsync(
            statement,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
