using Application.Common.Abstractions;
using Application.Common.Exceptions;
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
