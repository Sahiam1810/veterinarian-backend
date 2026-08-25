using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Roles.UseCase;

public sealed class UpdateRoleCommandHandler
    : IRequestHandler<UpdateRoleCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateRoleCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _uow.RolesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (role is null)
        {
            return false;
        }

        var roleExists = await _uow.RolesRepository.ExistsByNameAsync(
            request.Name,
            cancellationToken,
            request.Id);

        if (roleExists)
        {
            throw new ConflictException(
                "Ya existe un rol con ese nombre.");
        }

        role.Update(request.Name, request.Description);

        await _uow.RolesRepository.UpdateAsync(
            role,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
