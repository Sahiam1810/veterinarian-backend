using FluentValidation;
using Domain.Roles.ValueObjects;

namespace Application.Roles.UseCase;

public sealed class CreateRoleCommandValidator
    : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre del rol es obligatorio.")
            .MaximumLength(RoleName.MaxLength)
            .WithMessage(
                $"El nombre del rol no puede superar los {RoleName.MaxLength} caracteres.");
    }
}