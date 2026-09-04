using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Errors;
using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed class UpdateUserAccountCommandHandler
    : IRequestHandler<UpdateUserAccountCommand>
{
    private const string ClientRoleName = "Cliente";

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

        // Update no cambia UserId, pero reactivar/editar una account de Cliente
        // reabriría el agujero de login; misma regla que Create.
        var user = await _uow.UsersRepository.GetByIdAsync(
            account.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "El usuario asociado a la cuenta no existe.");
        }

        var role = await _uow.RolesRepository.GetByIdAsync(
            user.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new NotFoundException(
                "El rol del usuario asociado no existe.");
        }

        if (string.Equals(role.Name.Value, ClientRoleName, StringComparison.Ordinal))
        {
            throw new ConflictException(
                "Un usuario con rol Cliente no puede tener cuenta de acceso.",
                UserAccountErrorCodes.ClientCannotHaveLogin);
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
