using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Roles;
using MediatR;

namespace Application.Roles.UseCase;

public sealed class UpdateRoleCommandHandler
    : IRequestHandler<UpdateRoleCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateRoleCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _uow.RolesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Rol no encontrado.");

        if (SystemRoles.IsSuperAdmin(request.Id) ||
            SystemRoles.IsReservedName(request.Name))
        {
            throw new ForbiddenException(
                "El rol SuperAdmin no se puede modificar desde la administración de roles.");
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
    }
}
