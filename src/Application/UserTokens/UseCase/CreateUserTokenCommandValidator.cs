using FluentValidation;

namespace Application.UserTokens.UseCase;

public sealed class CreateUserTokenCommandValidator
    : AbstractValidator<CreateUserTokenCommand>
{
    public CreateUserTokenCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty()
            .WithMessage("Debe asociar el token a una cuenta.");

        RuleFor(command => command.TokenValue)
            .NotEmpty()
            .WithMessage("El valor del token es obligatorio.")
            .MaximumLength(500)
            .WithMessage("El valor del token no puede superar los 500 caracteres.");

        RuleFor(command => command.TokenType)
            .NotEmpty()
            .WithMessage("El tipo de token es obligatorio.")
            .MaximumLength(20)
            .WithMessage("El tipo de token no puede superar los 20 caracteres.");

        RuleFor(command => command.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("La fecha de expiración debe ser futura.");
    }
}
