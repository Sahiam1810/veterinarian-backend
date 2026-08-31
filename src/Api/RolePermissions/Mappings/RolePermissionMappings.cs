using Api.RolePermissions.Dtos;
using Domain.RolePermissions.Entities;

namespace Api.RolePermissions.Mappings;

// Mapeos de RolePermission a DTO.
public static class RolePermissionMappings
{
    public static RolePermissionResponseDto ToDto(this RolePermission entity) =>
        new(
            entity.Id,
            entity.RoleId,
            entity.ModuleId,
            entity.CanView,
            entity.CanCreate,
            entity.CanEdit,
            entity.CanDelete,
            entity.CreatedAt,
            entity.UpdatedAt);
}
