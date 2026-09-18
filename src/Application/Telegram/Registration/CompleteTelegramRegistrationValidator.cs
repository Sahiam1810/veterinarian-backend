using Domain.Clients.ValueObjects;
using FluentValidation;

namespace Application.Telegram.Registration;

// Validación del complete Telegram: sin password; teléfono obligatorio (ADR dueño).
public sealed class CompleteTelegramRegistrationCommandValidator
    : AbstractValidator<CompleteTelegramRegistrationCommand>
{
    public CompleteTelegramRegistrationCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(128);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.IdentificationNumber)
            .NotEmpty()
            .MaximumLength(ClientIdentificationNumber.MaxLength);
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .MaximumLength(ClientPhoneNumber.MaxLength);
        RuleFor(command => command.Address)
            .MaximumLength(ClientAddress.MaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Address));
    }
}
