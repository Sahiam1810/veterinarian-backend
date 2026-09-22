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
        // U4: el sub ya es el id del usuario; ya no hace falta pasar por UserAccounts.
        var user = await unitOfWork.UsersRepository.GetByIdAsync(
            request.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.SetPhotoUrl(request.PhotoUrl);

        await unitOfWork.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
