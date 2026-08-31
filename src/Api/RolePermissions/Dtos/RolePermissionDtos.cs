using System.ComponentModel.DataAnnotations;

namespace Api.RolePermissions.Dtos;

public sealed record CreateRolePermissionDto(
    [Required(ErrorMessage = "El rol es obligatorio.")]
    Guid RoleId,
    [Required(ErrorMessage = "El módulo es obligatorio.")]
    Guid ModuleId,
    bool CanView = false,
    bool CanCreate = false,
    bool CanEdit = false,
    bool CanDelete = false);

public sealed record UpdateRolePermissionDto(
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete);

public sealed record RolePermissionResponseDto(
    Guid Id,
    Guid RoleId,
    Guid ModuleId,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
