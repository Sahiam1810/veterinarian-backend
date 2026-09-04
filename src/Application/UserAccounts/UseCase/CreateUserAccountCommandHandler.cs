using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Errors;
using MediatR;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.UserAccounts.UseCase;

public sealed class CreateUserAccountCommandHandler
    : IRequestHandler<CreateUserAccountCommand, Guid>
{
    private const string ClientRoleName = "Cliente";

    private readonly IUnitOfWork _uow;

    public CreateUserAccountCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateUserAccountCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "El usuario especificado no existe.");
        }

        var role = await _uow.RolesRepository.GetByIdAsync(
            user.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new NotFoundException(
                "El rol del usuario especificado no existe.");
        }

        // Cliente no tiene login staff: no asociar USER_ACCOUNTS.
        if (string.Equals(role.Name.Value, ClientRoleName, StringComparison.Ordinal))
        {
            throw new ConflictException(
                "Un usuario con rol Cliente no puede tener cuenta de acceso.",
                UserAccountErrorCodes.ClientCannotHaveLogin);
        }

        var userAlreadyHasAccount = await _uow.UserAccountsRepository.ExistsByUserIdAsync(
            request.UserId,
            cancellationToken);

        if (userAlreadyHasAccount)
        {
            throw new ConflictException(
                "El usuario ya tiene una cuenta asociada.");
        }

        var usernameInUse = await _uow.UserAccountsRepository.ExistsByUsernameAsync(
            request.Username,
            cancellationToken);

        if (usernameInUse)
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese nombre de usuario.");
        }

        var mailInUse = await _uow.UserAccountsRepository.ExistsByMailAsync(
            request.Mail,
            cancellationToken);

        if (mailInUse)
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese correo electrónico.");
        }

        var account = new UserAccountEntity(
            request.UserId,
            request.Username,
            request.Mail,
            request.Status);

        await _uow.UserAccountsRepository.AddAsync(
            account,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
