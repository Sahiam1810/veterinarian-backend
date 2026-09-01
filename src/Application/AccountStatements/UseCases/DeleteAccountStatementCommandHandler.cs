using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.AccountStatements.UseCases;

public sealed class DeleteAccountStatementCommandHandler
    : IRequestHandler<DeleteAccountStatementCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteAccountStatementCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteAccountStatementCommand request,
        CancellationToken cancellationToken)
    {
        var statement = await _uow.AccountStatementsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Estado de cuenta no encontrado.");

        await _uow.AccountStatementsRepository.DeleteAsync(
            statement,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
