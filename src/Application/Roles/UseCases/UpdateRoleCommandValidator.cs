using FluentValidation;
using Domain.Roles.ValueObjects;

namespace Application.Roles.UseCase;

public sealed class UpdateRoleCommandValidator
    : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre del rol es obligatorio.")
            .MaximumLength(RoleName.MaxLength)
            .WithMessage(
                $"El nombre del rol no puede superar los {RoleName.MaxLength} caracteres.");
    }
}
