using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed class UpdateUserAccountCommandHandler
    : IRequestHandler<UpdateUserAccountCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateUserAccountCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        UpdateUserAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var usernameInUse = await _uow.UserAccountsRepository.ExistsByUsernameAsync(
            request.Username,
            cancellationToken,
            request.Id);

        if (usernameInUse)
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese nombre de usuario.");
        }

        var mailInUse = await _uow.UserAccountsRepository.ExistsByMailAsync(
            request.Mail,
            cancellationToken,
            request.Id);

        if (mailInUse)
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese correo electrónico.");
        }

        account.Update(request.Username, request.Mail, request.Status);

        await _uow.UserAccountsRepository.UpdateAsync(
            account,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
