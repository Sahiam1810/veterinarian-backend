using Domain.Clients.ValueObjects;
using FluentValidation;

namespace Application.Clients.UseCases;

public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("El usuario es obligatorio.");

        RuleFor(command => command.IdentificationNumber)
            .NotEmpty()
            .WithMessage("El número de identificación es obligatorio.")
            .MaximumLength(ClientIdentificationNumber.MaxLength)
            .WithMessage(
                $"El número de identificación no puede superar los {ClientIdentificationNumber.MaxLength} caracteres.");

        RuleFor(command => command.Address)
            .MaximumLength(ClientAddress.MaxLength)
            .WithMessage(
                $"La dirección no puede superar los {ClientAddress.MaxLength} caracteres.");

        RuleFor(command => command.PhoneNumber)
            .RequiredPhoneNumber();
    }
}
