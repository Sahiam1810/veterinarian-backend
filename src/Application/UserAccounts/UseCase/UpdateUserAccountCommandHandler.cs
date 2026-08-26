using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed class UpdateUserAccountCommandHandler
    : IRequestHandler<UpdateUserAccountCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateUserAccountCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        UpdateUserAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (account is null)
        {
            return false;
        }

        var usernameInUse = await _uow.UserAccountsRepository.ExistsByUsernameAsync(
            request.Username,
            cancellationToken,
            request.Id);

        if (usernameInUse)
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese nombre de usuario.");
        }

        account.Update(request.Username, request.Mail, request.Status);

        await _uow.UserAccountsRepository.UpdateAsync(
            account,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
