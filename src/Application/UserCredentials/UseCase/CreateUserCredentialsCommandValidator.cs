using FluentValidation;

namespace Application.UserCredentials.UseCase;

public sealed class CreateUserCredentialsCommandValidator
    : AbstractValidator<CreateUserCredentialsCommand>
{
    public CreateUserCredentialsCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty()
            .WithMessage("Debe asociar las credenciales a una cuenta.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}
