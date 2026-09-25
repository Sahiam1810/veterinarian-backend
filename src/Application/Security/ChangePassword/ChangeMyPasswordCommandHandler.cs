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
        // U5: la contraseña vive directamente en USERS.
        var user = await unitOfWork.UsersRepository.GetByIdAsync(
            request.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException(
                "La contraseña actual no es correcta.");
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);

        user.ChangePassword(newPasswordHash);

        await unitOfWork.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
