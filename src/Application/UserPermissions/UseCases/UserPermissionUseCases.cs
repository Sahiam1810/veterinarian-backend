using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.UserPermissions.Entities;
using MediatR;

namespace Application.UserPermissions.UseCases;

public sealed record CreateUserPermissionCommand(
    Guid UserId,
    Guid ModuleId,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete) : IRequest<Guid>;

public sealed record GetAllUserPermissionsQuery
    : IRequest<IReadOnlyCollection<UserPermission>>;

public sealed record GetUserPermissionByIdQuery(Guid Id) : IRequest<UserPermission>;

public sealed record GetUserPermissionsByUserIdQuery(Guid UserId)
    : IRequest<IReadOnlyCollection<UserPermission>>;

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

// Lista todos los permisos puntuales.
public sealed class GetAllUserPermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllUserPermissionsQuery, IReadOnlyCollection<UserPermission>>
{
    public Task<IReadOnlyCollection<UserPermission>> Handle(
        GetAllUserPermissionsQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.UserPermissionsRepository.GetAllAsync(cancellationToken);
}

// Obtiene un permiso puntual por id.
public sealed class GetUserPermissionByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserPermissionByIdQuery, UserPermission>
{
    public async Task<UserPermission> Handle(
        GetUserPermissionByIdQuery request,
        CancellationToken cancellationToken) =>
        await unitOfWork.UserPermissionsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Permiso de usuario no encontrado.");
}

// Lista los permisos puntuales de un usuario.
public sealed class GetUserPermissionsByUserIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserPermissionsByUserIdQuery, IReadOnlyCollection<UserPermission>>
{
    public Task<IReadOnlyCollection<UserPermission>> Handle(
        GetUserPermissionsByUserIdQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.UserPermissionsRepository.GetByUserIdAsync(request.UserId, cancellationToken);
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
