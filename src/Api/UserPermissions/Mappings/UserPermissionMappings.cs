using Api.UserPermissions.Dtos;
using Application.UserPermissions.UseCases;

namespace Api.UserPermissions.Mappings;

// Mapeos de UserPermissionDetail a DTO.
public static class UserPermissionMappings
{
    public static UserPermissionResponseDto ToDto(this UserPermissionDetail detail) =>
        new(
            detail.Id,
            detail.UserId,
            detail.UserFullName,
            detail.UserEmail,
            detail.ModuleId,
            detail.ModuleName,
            detail.CanView,
            detail.CanCreate,
            detail.CanEdit,
            detail.CanDelete,
            detail.CreatedAt,
            detail.UpdatedAt);
}
