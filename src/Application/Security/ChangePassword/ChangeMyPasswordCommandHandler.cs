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
        // U4: el sub ya es el id del usuario; hay que resolver la cuenta antes de
        // llegar a las credenciales (U5 fusionará estas tres tablas en una).
        var account = await unitOfWork.UserAccountsRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var credentials = await unitOfWork.UserCredentialsRepository.GetByAccountIdAsync(
            account.Id,
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
