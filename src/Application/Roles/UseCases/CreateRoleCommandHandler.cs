using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using RoleEntity = Domain.Roles.Entities.Roles;

namespace Application.Roles.UseCase;

public sealed class CreateRoleCommandHandler
    : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateRoleCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var roleExists = await _uow.RolesRepository.ExistsByNameAsync(
            request.Name,
            cancellationToken);

        if (roleExists)
        {
            throw new ConflictException(
                "Ya existe un rol con ese nombre.");
        }

        var role = new RoleEntity(
            request.Name,
            request.Description);

        await _uow.RolesRepository.AddAsync(
            role,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}