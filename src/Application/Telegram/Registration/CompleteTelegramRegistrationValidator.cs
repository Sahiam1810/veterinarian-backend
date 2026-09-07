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
        RuleFor(command => command.IdentificationNumber)
            .NotEmpty()
            .MaximumLength(ClientIdentificationNumber.MaxLength);
    }
}
