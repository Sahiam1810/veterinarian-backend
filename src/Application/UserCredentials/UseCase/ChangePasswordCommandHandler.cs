using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.UserCredentials.UseCase;

public sealed class ChangePasswordCommandHandler
    : IRequestHandler<ChangePasswordCommand, bool>
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

    public async Task<bool> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var credentials = await _uow.UserCredentialsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (credentials is null)
        {
            return false;
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

        return true;
    }
}
