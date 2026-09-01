namespace Api.Auth.Dtos;

public sealed record UserPermissionsResponseDto(
    Dictionary<string, ModulePermissionDto> Permissions);

public sealed record ModulePermissionDto(
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete);
