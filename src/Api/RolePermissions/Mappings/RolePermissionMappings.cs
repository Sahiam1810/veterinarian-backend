using Api.RolePermissions.Dtos;
using Application.RolePermissions.UseCases;

namespace Api.RolePermissions.Mappings;

// Mapeos de RolePermissionDetail a DTO.
public static class RolePermissionMappings
{
    public static RolePermissionResponseDto ToDto(this RolePermissionDetail detail) =>
        new(
            detail.Id,
            detail.RoleId,
            detail.RoleName,
            detail.ModuleId,
            detail.ModuleName,
            detail.CanView,
            detail.CanCreate,
            detail.CanEdit,
            detail.CanDelete,
            detail.CreatedAt,
            detail.UpdatedAt);
}
