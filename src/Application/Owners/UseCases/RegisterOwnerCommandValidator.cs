using Application.Clients.Errors;
using Domain.Clients.ValueObjects;
using Domain.Users.ValueObjects;
using FluentValidation;

namespace Application.Owners.UseCases;

public sealed class RegisterOwnerCommandValidator : AbstractValidator<RegisterOwnerCommand>
{
    public RegisterOwnerCommandValidator()
    {
        RuleFor(command => command.FullName)
            .NotEmpty()
            .WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(150);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(UserEmail.MaxLength);

        RuleFor(command => command.IdentificationNumber)
            .NotEmpty()
            .MaximumLength(ClientIdentificationNumber.MaxLength);

        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .WithErrorCode(ClientErrorCodes.PhoneRequired)
            .Must(value => ClientPhoneNumber.TryCreate(value, out _))
            .WithErrorCode(ClientErrorCodes.PhoneInvalidFormat);

        RuleFor(command => command.Channel)
            .IsInEnum()
            .WithMessage("El canal de registro no es válido.");
    }
}
