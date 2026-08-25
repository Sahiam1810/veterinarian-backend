using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Users.UseCase;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        var role = await _uow.RolesRepository.GetByIdAsync(
            request.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new NotFoundException(
                "El rol especificado no existe.");
        }

        var emailInUse = await _uow.UsersRepository.ExistsByEmailAsync(
            request.Email,
            cancellationToken,
            request.Id);

        if (emailInUse)
        {
            throw new ConflictException(
                "Ya existe un usuario con ese correo electrónico.");
        }

        user.Update(request.FullName, request.Email, request.RoleId);

        await _uow.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
