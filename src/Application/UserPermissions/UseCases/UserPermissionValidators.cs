using FluentValidation;

namespace Application.UserPermissions.UseCases;

// Valida creación de permiso puntual.
public sealed class CreateUserPermissionCommandValidator : AbstractValidator<CreateUserPermissionCommand>
{
    public CreateUserPermissionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
    }
}

// Valida actualización de permiso puntual.
public sealed class UpdateUserPermissionCommandValidator : AbstractValidator<UpdateUserPermissionCommand>
{
    public UpdateUserPermissionCommandValidator() =>
        RuleFor(x => x.Id).NotEmpty();
}
