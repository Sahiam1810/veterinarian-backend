using Application.Common.Abstractions;
using Application.Common.Exceptions;
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
