using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.RolePermissions.Entities;
using MediatR;

namespace Application.RolePermissions.UseCases;

public sealed record CreateRolePermissionCommand(
    Guid RoleId,
    Guid ModuleId,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete) : IRequest<Guid>;

public sealed record GetAllRolePermissionsQuery
    : IRequest<IReadOnlyCollection<RolePermission>>;

public sealed record GetRolePermissionByIdQuery(Guid Id) : IRequest<RolePermission>;

public sealed record GetRolePermissionsByRoleIdQuery(Guid RoleId)
    : IRequest<IReadOnlyCollection<RolePermission>>;

public sealed record UpdateRolePermissionCommand(
    Guid Id,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete) : IRequest;

public sealed record DeleteRolePermissionCommand(Guid Id) : IRequest;

// Crea un permiso de rol sobre un módulo.
public sealed class CreateRolePermissionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRolePermissionCommand, Guid>
{
    public async Task<Guid> Handle(CreateRolePermissionCommand request, CancellationToken cancellationToken)
    {
        _ = await unitOfWork.RolesRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Rol no encontrado.");

        _ = await unitOfWork.ModulesRepository.GetByIdAsync(request.ModuleId, cancellationToken)
            ?? throw new NotFoundException("Módulo no encontrado.");

        var existing = await unitOfWork.RolePermissionsRepository.GetByRoleAndModuleIdAsync(
            request.RoleId,
            request.ModuleId,
            cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException("Ya existe un permiso para ese rol y módulo.");
        }

        var permission = new RolePermission(
            request.RoleId,
            request.ModuleId,
            request.CanView,
            request.CanCreate,
            request.CanEdit,
            request.CanDelete);

        await unitOfWork.RolePermissionsRepository.AddAsync(permission, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return permission.Id;
    }
}

// Lista todos los permisos.
public sealed class GetAllRolePermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllRolePermissionsQuery, IReadOnlyCollection<RolePermission>>
{
    public Task<IReadOnlyCollection<RolePermission>> Handle(
        GetAllRolePermissionsQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.RolePermissionsRepository.GetAllAsync(cancellationToken);
}

// Obtiene un permiso por id.
public sealed class GetRolePermissionByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRolePermissionByIdQuery, RolePermission>
{
    public async Task<RolePermission> Handle(
        GetRolePermissionByIdQuery request,
        CancellationToken cancellationToken) =>
        await unitOfWork.RolePermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de rol no encontrado.");
}

// Lista permisos de un rol.
public sealed class GetRolePermissionsByRoleIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRolePermissionsByRoleIdQuery, IReadOnlyCollection<RolePermission>>
{
    public Task<IReadOnlyCollection<RolePermission>> Handle(
        GetRolePermissionsByRoleIdQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.RolePermissionsRepository.GetByRoleIdAsync(request.RoleId, cancellationToken);
}

// Actualiza flags de permiso.
public sealed class UpdateRolePermissionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateRolePermissionCommand>
{
    public async Task Handle(UpdateRolePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await unitOfWork.RolePermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de rol no encontrado.");

        permission.UpdatePermissions(
            request.CanView,
            request.CanCreate,
            request.CanEdit,
            request.CanDelete);

        await unitOfWork.RolePermissionsRepository.UpdateAsync(permission, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// Elimina un permiso de rol.
public sealed class DeleteRolePermissionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRolePermissionCommand>
{
    public async Task Handle(DeleteRolePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await unitOfWork.RolePermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de rol no encontrado.");

        await unitOfWork.RolePermissionsRepository.DeleteAsync(permission, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
