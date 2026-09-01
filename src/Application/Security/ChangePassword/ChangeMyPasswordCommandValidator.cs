using FluentValidation;

namespace Application.Security.ChangePassword;

public sealed class ChangeMyPasswordCommandValidator
    : AbstractValidator<ChangeMyPasswordCommand>
{
    public ChangeMyPasswordCommandValidator()
    {
        RuleFor(command => command.UserAccountId)
            .NotEmpty();

        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithMessage("Debe indicar la contraseña actual.");

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(8)
            .WithMessage("La nueva contraseña debe tener al menos 8 caracteres.");
    }
}
