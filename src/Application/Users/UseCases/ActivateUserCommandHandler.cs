using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.UserAccounts.ValueObjects;
using MediatR;

namespace Application.Users.UseCase;

public sealed class ActivateUserCommandHandler
    : IRequestHandler<ActivateUserCommand>
{
    private readonly IUnitOfWork _uow;

    public ActivateUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        ActivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.Activate();

        await _uow.ExecuteInTransactionAsync(async transactionToken =>
        {
            await _uow.UsersRepository.UpdateAsync(user, transactionToken);

            var account = await _uow.UserAccountsRepository.GetByUserIdAsync(
                user.Id, transactionToken);

            if (account is not null)
            {
                account.Update(
                    account.Username.Value,
                    account.Mail.Value,
                    AccountStatus.Active);

                await _uow.UserAccountsRepository.UpdateAsync(account, transactionToken);
            }

            await _uow.SaveChangesAsync(transactionToken);
        }, cancellationToken);
    }
}
