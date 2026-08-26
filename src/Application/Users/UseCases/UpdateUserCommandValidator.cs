using Domain.Users.ValueObjects;
using FluentValidation;

namespace Application.Users.UseCase;

public sealed class UpdateUserCommandValidator
    : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.FullName)
            .NotEmpty()
            .WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(150)
            .WithMessage("El nombre completo no puede superar los 150 caracteres.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(UserEmail.MaxLength)
            .WithMessage(
                $"El correo electrónico no puede superar los {UserEmail.MaxLength} caracteres.")
            .EmailAddress()
            .WithMessage("El correo electrónico no tiene un formato válido.");

        RuleFor(command => command.RoleId)
            .NotEmpty()
            .WithMessage("Debe asignar un rol al usuario.");
    }
}
