using Api.UserPermissions.Dtos;
using Domain.UserPermissions.Entities;

namespace Api.UserPermissions.Mappings;

// Mapeos de UserPermission a DTO.
public static class UserPermissionMappings
{
    public static UserPermissionResponseDto ToDto(this UserPermission entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.ModuleId,
            entity.CanView,
            entity.CanCreate,
            entity.CanEdit,
            entity.CanDelete,
            entity.CreatedAt,
            entity.UpdatedAt);
}
