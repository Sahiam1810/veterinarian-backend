using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.AccountStatements.UseCases;

public sealed class UpdateAccountStatementStatusCommandHandler
    : IRequestHandler<UpdateAccountStatementStatusCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateAccountStatementStatusCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        UpdateAccountStatementStatusCommand request,
        CancellationToken cancellationToken)
    {
        var statement = await _uow.AccountStatementsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Estado de cuenta no encontrado.");

        statement.UpdateStatus(request.Status);

        await _uow.AccountStatementsRepository.UpdateAsync(
            statement,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
