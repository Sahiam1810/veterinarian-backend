using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.RolePermissions.Entities;
using MediatR;

namespace Application.RolePermissions.UseCases;

internal static class RolePermissionDetailMappings
{
    public static RolePermissionDetail ToDetail(
        this RolePermission permission,
        string roleName,
        string moduleName) =>
        new(
            permission.Id,
            permission.RoleId,
            roleName,
            permission.ModuleId,
            moduleName,
            permission.CanView,
            permission.CanCreate,
            permission.CanEdit,
            permission.CanDelete,
            permission.CreatedAt,
            permission.UpdatedAt);

    public static RolePermissionDetail ToDetail(
        this RolePermission permission,
        IReadOnlyDictionary<Guid, string> roleNames,
        IReadOnlyDictionary<Guid, string> moduleNames) =>
        permission.ToDetail(
            roleNames.GetValueOrDefault(permission.RoleId, string.Empty),
            moduleNames.GetValueOrDefault(permission.ModuleId, string.Empty));

    public static RolePermissionDetail ToDetail(
        this RolePermission permission,
        string roleName,
        IReadOnlyDictionary<Guid, string> moduleNames) =>
        permission.ToDetail(
            roleName,
            moduleNames.GetValueOrDefault(permission.ModuleId, string.Empty));
}

public sealed record CreateRolePermissionCommand(
    Guid RoleId,
    Guid ModuleId,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete) : IRequest<Guid>;

// Incluye RoleName/ModuleName resueltos, para que el consumidor (UI de SuperAdmin)
// no tenga que cruzar cada fila contra GET /api/roles y GET /api/modules a mano.
public sealed record RolePermissionDetail(
    Guid Id,
    Guid RoleId,
    string RoleName,
    Guid ModuleId,
    string ModuleName,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record GetAllRolePermissionsQuery
    : IRequest<IReadOnlyCollection<RolePermissionDetail>>;

public sealed record GetRolePermissionByIdQuery(Guid Id) : IRequest<RolePermissionDetail>;

public sealed record GetRolePermissionsByRoleIdQuery(Guid RoleId)
    : IRequest<IReadOnlyCollection<RolePermissionDetail>>;

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

// Lista todos los permisos, con RoleName/ModuleName resueltos.
public sealed class GetAllRolePermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllRolePermissionsQuery, IReadOnlyCollection<RolePermissionDetail>>
{
    public async Task<IReadOnlyCollection<RolePermissionDetail>> Handle(
        GetAllRolePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await unitOfWork.RolePermissionsRepository.GetAllAsync(cancellationToken);
        var roleNames = (await unitOfWork.RolesRepository.GetAllAsync(cancellationToken))
            .ToDictionary(role => role.Id, role => role.Name.Value);
        var moduleNames = (await unitOfWork.ModulesRepository.GetAllAsync(cancellationToken))
            .ToDictionary(module => module.Id, module => module.Name.Value);

        return permissions
            .Select(permission => permission.ToDetail(roleNames, moduleNames))
            .ToArray();
    }
}

// Obtiene un permiso por id, con RoleName/ModuleName resueltos.
public sealed class GetRolePermissionByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRolePermissionByIdQuery, RolePermissionDetail>
{
    public async Task<RolePermissionDetail> Handle(
        GetRolePermissionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var permission = await unitOfWork.RolePermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de rol no encontrado.");

        var role = await unitOfWork.RolesRepository.GetByIdAsync(permission.RoleId, cancellationToken);
        var module = await unitOfWork.ModulesRepository.GetByIdAsync(permission.ModuleId, cancellationToken);

        return permission.ToDetail(role?.Name.Value ?? string.Empty, module?.Name.Value ?? string.Empty);
    }
}

// Lista permisos de un rol, con RoleName/ModuleName resueltos.
public sealed class GetRolePermissionsByRoleIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRolePermissionsByRoleIdQuery, IReadOnlyCollection<RolePermissionDetail>>
{
    public async Task<IReadOnlyCollection<RolePermissionDetail>> Handle(
        GetRolePermissionsByRoleIdQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await unitOfWork.RolePermissionsRepository.GetByRoleIdAsync(
            request.RoleId,
            cancellationToken);
        var role = await unitOfWork.RolesRepository.GetByIdAsync(request.RoleId, cancellationToken);
        var roleName = role?.Name.Value ?? string.Empty;
        var moduleNames = (await unitOfWork.ModulesRepository.GetAllAsync(cancellationToken))
            .ToDictionary(module => module.Id, module => module.Name.Value);

        return permissions
            .Select(permission => permission.ToDetail(roleName, moduleNames))
            .ToArray();
    }
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
