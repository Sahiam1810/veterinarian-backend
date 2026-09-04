using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Security;
using Application.Security.Errors;
using Domain.UserAccounts.ValueObjects;
using MediatR;

namespace Application.UserCredentials.UseCase;

public sealed class ChangePasswordCommandHandler
    : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var credentials = await _uow.UserCredentialsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Credenciales no encontradas.");

        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            credentials.AccountId,
            cancellationToken)
            ?? throw new NotFoundException("La cuenta asociada no existe.");

        var user = await _uow.UsersRepository.GetByIdAsync(
            account.UserId,
            cancellationToken)
            ?? throw new NotFoundException("El usuario de la cuenta no existe.");

        var role = await _uow.RolesRepository.GetByIdAsync(
            user.RoleId,
            cancellationToken)
            ?? throw new NotFoundException("El rol del usuario no existe.");

        // Misma regla que Login/Refresh: solo rol de panel web con cuenta activa
        // puede modificar USER_CREDENTIALS de plataforma.
        if (!string.Equals(account.Status, AccountStatus.Active, StringComparison.Ordinal)
            || !WebPlatformAccess.IsAllowedRoleName(role.Name.Value))
        {
            throw new ForbiddenException(AuthenticationErrors.PlatformAccessDenied);
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, credentials.PasswordHash))
        {
            throw new UnauthorizedException(
                "La contraseña actual no es correcta.");
        }

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);

        credentials.ChangePassword(newPasswordHash);

        await _uow.UserCredentialsRepository.UpdateAsync(
            credentials,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
