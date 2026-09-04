using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.UserAccounts.ValueObjects;
using Domain.Roles;
using MediatR;

namespace Application.Users.UseCase;

public sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand>
{
    private readonly IUnitOfWork _uow;

    public DeactivateUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        if (SystemRoles.IsSuperAdmin(user.RoleId))
        {
            throw new ForbiddenException(
                "El usuario SuperAdmin no se puede desactivar desde la administración de usuarios.");
        }

        user.Deactivate();

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
                    AccountStatus.Inactive);

                await _uow.UserAccountsRepository.UpdateAsync(account, transactionToken);

                var tokens = await _uow.UserTokensRepository.GetAllByAccountIdAsync(
                    account.Id, transactionToken);

                foreach (var token in tokens)
                {
                    await _uow.UserTokensRepository.DeleteAsync(token, transactionToken);
                }
            }

            await _uow.SaveChangesAsync(transactionToken);
        }, cancellationToken);
    }
}
