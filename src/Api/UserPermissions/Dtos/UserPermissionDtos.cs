using System.ComponentModel.DataAnnotations;

namespace Api.UserPermissions.Dtos;

public sealed record CreateUserPermissionDto(
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    Guid UserId,
    [Required(ErrorMessage = "El módulo es obligatorio.")]
    Guid ModuleId,
    bool CanView = false,
    bool CanCreate = false,
    bool CanEdit = false,
    bool CanDelete = false);

public sealed record UpdateUserPermissionDto(
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete);

public sealed record UserPermissionResponseDto(
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
