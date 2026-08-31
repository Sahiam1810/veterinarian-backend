using FluentValidation;

namespace Application.RolePermissions.UseCases;

// Valida creación de permiso por rol.
public sealed class CreateRolePermissionCommandValidator : AbstractValidator<CreateRolePermissionCommand>
{
    public CreateRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
    }
}

// Valida actualización de permiso por rol.
public sealed class UpdateRolePermissionCommandValidator : AbstractValidator<UpdateRolePermissionCommand>
{
    public UpdateRolePermissionCommandValidator() =>
        RuleFor(x => x.Id).NotEmpty();
}
