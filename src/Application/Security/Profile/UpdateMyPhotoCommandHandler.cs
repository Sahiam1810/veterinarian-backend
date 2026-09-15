using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Security.Profile;

public sealed class UpdateMyPhotoCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMyPhotoCommand>
{
    public async Task Handle(
        UpdateMyPhotoCommand request,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var user = await unitOfWork.UsersRepository.GetByIdAsync(
            account.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.SetPhotoUrl(request.PhotoUrl);

        await unitOfWork.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
