using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Roles;
using MediatR;

namespace Application.Users.UseCase;

public sealed class ResetUserPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ResetUserPasswordCommand>
{
    public async Task Handle(
        ResetUserPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        if (SystemRoles.IsSuperAdmin(user.RoleId))
        {
            throw new ForbiddenException(
                "La contraseña de SuperAdmin no se puede restablecer desde la administración de usuarios.");
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);

        user.ChangePassword(newPasswordHash);

        await unitOfWork.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
