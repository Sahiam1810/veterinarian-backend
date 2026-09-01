using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Security.ChangePassword;

public sealed class ChangeMyPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ChangeMyPasswordCommand>
{
    public async Task Handle(
        ChangeMyPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var credentials = await unitOfWork.UserCredentialsRepository.GetByAccountIdAsync(
            request.UserAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Credenciales no encontradas.");

        if (!passwordHasher.Verify(request.CurrentPassword, credentials.PasswordHash))
        {
            throw new UnauthorizedException(
                "La contraseña actual no es correcta.");
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);

        credentials.ChangePassword(newPasswordHash);

        await unitOfWork.UserCredentialsRepository.UpdateAsync(
            credentials,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
