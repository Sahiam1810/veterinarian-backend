using Application.Common.Abstractions;
using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed class DeleteUserAccountCommandHandler
    : IRequestHandler<DeleteUserAccountCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteUserAccountCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        DeleteUserAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (account is null)
        {
            return false;
        }

        await _uow.UserAccountsRepository.DeleteAsync(
            account,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
