using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.UserPermissions.Entities;
using MediatR;

namespace Application.UserPermissions.UseCases;

internal static class UserPermissionDetailMappings
{
    public static UserPermissionDetail ToDetail(
        this UserPermission permission,
        string userFullName,
        string userEmail,
        string moduleName) =>
        new(
            permission.Id,
            permission.UserId,
            userFullName,
            userEmail,
            permission.ModuleId,
            moduleName,
            permission.CanView,
            permission.CanCreate,
            permission.CanEdit,
            permission.CanDelete,
            permission.CreatedAt,
            permission.UpdatedAt);

    public static UserPermissionDetail ToDetail(
        this UserPermission permission,
        IReadOnlyDictionary<Guid, (string FullName, string Email)> users,
        IReadOnlyDictionary<Guid, string> moduleNames)
    {
        var (fullName, email) = users.GetValueOrDefault(permission.UserId, (string.Empty, string.Empty));
        return permission.ToDetail(
            fullName,
            email,
            moduleNames.GetValueOrDefault(permission.ModuleId, string.Empty));
    }
}

public sealed record CreateUserPermissionCommand(
    Guid UserId,
    Guid ModuleId,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete) : IRequest<Guid>;

// Incluye UserFullName/UserEmail/ModuleName resueltos, para que el consumidor
// (UI de SuperAdmin) no tenga que cruzar cada fila contra GET /api/users y
// GET /api/modules a mano.
public sealed record UserPermissionDetail(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    Guid ModuleId,
    string ModuleName,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record GetAllUserPermissionsQuery
    : IRequest<IReadOnlyCollection<UserPermissionDetail>>;

public sealed record GetUserPermissionByIdQuery(Guid Id) : IRequest<UserPermissionDetail>;

public sealed record GetUserPermissionsByUserIdQuery(Guid UserId)
    : IRequest<IReadOnlyCollection<UserPermissionDetail>>;

public sealed record UpdateUserPermissionCommand(
    Guid Id,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete) : IRequest;

public sealed record DeleteUserPermissionCommand(Guid Id) : IRequest;

// Crea un permiso puntual de un usuario sobre un módulo (se suma al de su rol).
public sealed class CreateUserPermissionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateUserPermissionCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserPermissionCommand request, CancellationToken cancellationToken)
    {
        _ = await unitOfWork.UsersRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        _ = await unitOfWork.ModulesRepository.GetByIdAsync(request.ModuleId, cancellationToken)
            ?? throw new NotFoundException("Módulo no encontrado.");

        var existing = await unitOfWork.UserPermissionsRepository.GetByUserAndModuleIdAsync(
            request.UserId,
            request.ModuleId,
            cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException("Ya existe un permiso para ese usuario y módulo.");
        }

        var permission = new UserPermission(
            request.UserId,
            request.ModuleId,
            request.CanView,
            request.CanCreate,
            request.CanEdit,
            request.CanDelete);

        await unitOfWork.UserPermissionsRepository.AddAsync(permission, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return permission.Id;
    }
}

// Lista todos los permisos puntuales, con UserFullName/UserEmail/ModuleName resueltos.
public sealed class GetAllUserPermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllUserPermissionsQuery, IReadOnlyCollection<UserPermissionDetail>>
{
    public async Task<IReadOnlyCollection<UserPermissionDetail>> Handle(
        GetAllUserPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await unitOfWork.UserPermissionsRepository.GetAllAsync(cancellationToken);
        var users = (await unitOfWork.UsersRepository.GetAllAsync(cancellationToken))
            .ToDictionary(user => user.Id, user => (user.FullName, user.Email.Value));
        var moduleNames = (await unitOfWork.ModulesRepository.GetAllAsync(cancellationToken))
            .ToDictionary(module => module.Id, module => module.Name.Value);

        return permissions
            .Select(permission => permission.ToDetail(users, moduleNames))
            .ToArray();
    }
}

// Obtiene un permiso puntual por id, con UserFullName/UserEmail/ModuleName resueltos.
public sealed class GetUserPermissionByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserPermissionByIdQuery, UserPermissionDetail>
{
    public async Task<UserPermissionDetail> Handle(
        GetUserPermissionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var permission = await unitOfWork.UserPermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de usuario no encontrado.");

        var user = await unitOfWork.UsersRepository.GetByIdAsync(permission.UserId, cancellationToken);
        var module = await unitOfWork.ModulesRepository.GetByIdAsync(permission.ModuleId, cancellationToken);

        return permission.ToDetail(
            user?.FullName ?? string.Empty,
            user?.Email.Value ?? string.Empty,
            module?.Name.Value ?? string.Empty);
    }
}

// Lista los permisos puntuales de un usuario, con UserFullName/UserEmail/ModuleName resueltos.
public sealed class GetUserPermissionsByUserIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserPermissionsByUserIdQuery, IReadOnlyCollection<UserPermissionDetail>>
{
    public async Task<IReadOnlyCollection<UserPermissionDetail>> Handle(
        GetUserPermissionsByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await unitOfWork.UserPermissionsRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);
        var user = await unitOfWork.UsersRepository.GetByIdAsync(request.UserId, cancellationToken);
        var userFullName = user?.FullName ?? string.Empty;
        var userEmail = user?.Email.Value ?? string.Empty;
        var moduleNames = (await unitOfWork.ModulesRepository.GetAllAsync(cancellationToken))
            .ToDictionary(module => module.Id, module => module.Name.Value);

        return permissions
            .Select(permission => permission.ToDetail(userFullName, userEmail, moduleNames.GetValueOrDefault(permission.ModuleId, string.Empty)))
            .ToArray();
    }
}

// Actualiza flags de un permiso puntual.
public sealed class UpdateUserPermissionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserPermissionCommand>
{
    public async Task Handle(UpdateUserPermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await unitOfWork.UserPermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de usuario no encontrado.");

        permission.UpdatePermissions(
            request.CanView,
            request.CanCreate,
            request.CanEdit,
            request.CanDelete);

        await unitOfWork.UserPermissionsRepository.UpdateAsync(permission, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// Elimina un permiso puntual.
public sealed class DeleteUserPermissionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserPermissionCommand>
{
    public async Task Handle(DeleteUserPermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await unitOfWork.UserPermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de usuario no encontrado.");

        await unitOfWork.UserPermissionsRepository.DeleteAsync(permission, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
