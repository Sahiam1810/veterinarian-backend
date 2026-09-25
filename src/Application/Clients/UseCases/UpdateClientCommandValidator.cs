using Domain.Clients.ValueObjects;
using FluentValidation;

namespace Application.Clients.UseCases;

public sealed class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.FullName)
            .NotEmpty()
            .WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(ClientFullName.MaxLength)
            .WithMessage(
                $"El nombre completo no puede superar los {ClientFullName.MaxLength} caracteres.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .Must(ClientEmail.IsValidFormat)
            .WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(ClientEmail.MaxLength)
            .WithMessage(
                $"El correo electrónico no puede superar los {ClientEmail.MaxLength} caracteres.");

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
