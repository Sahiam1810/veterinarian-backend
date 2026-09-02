using Domain.Clients.ValueObjects;
using FluentValidation;

namespace Application.Telegram.Registration;

public sealed class CompleteTelegramRegistrationCommandValidator
    : AbstractValidator<CompleteTelegramRegistrationCommand>
{
    public CompleteTelegramRegistrationCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(128);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.UserName).NotEmpty().MaximumLength(50);
        RuleFor(command => command.IdentificationNumber)
            .NotEmpty()
            .MaximumLength(ClientIdentificationNumber.MaxLength);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(command => command.PasswordConfirmation)
            .Equal(command => command.Password)
            .WithMessage("Las contraseñas no coinciden.");
    }
}
