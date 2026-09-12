using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Roles;
using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed class DeleteUserAccountCommandHandler
    : IRequestHandler<DeleteUserAccountCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteUserAccountCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteUserAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var user = await _uow.UsersRepository.GetByIdAsync(
            account.UserId,
            cancellationToken)
            ?? throw new NotFoundException("El usuario asociado a la cuenta no existe.");

        if (SystemRoles.IsSuperAdmin(user.RoleId))
        {
            throw new ForbiddenException(
                "La cuenta SuperAdmin no se puede eliminar.");
        }

        await _uow.UserAccountsRepository.DeleteAsync(
            account,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
