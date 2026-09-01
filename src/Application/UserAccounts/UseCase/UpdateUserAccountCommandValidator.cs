using Domain.UserAccounts.ValueObjects;
using FluentValidation;

namespace Application.UserAccounts.UseCase;

public sealed class UpdateUserAccountCommandValidator
    : AbstractValidator<UpdateUserAccountCommand>
{
    public UpdateUserAccountCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Username)
            .NotEmpty()
            .WithMessage("El nombre de usuario es obligatorio.")
            .MaximumLength(AccountUsername.MaxLength)
            .WithMessage(
                $"El nombre de usuario no puede superar los {AccountUsername.MaxLength} caracteres.");

        RuleFor(command => command.Mail)
            .NotEmpty()
            .WithMessage("El correo de la cuenta es obligatorio.")
            .MaximumLength(AccountMail.MaxLength)
            .WithMessage(
                $"El correo de la cuenta no puede superar los {AccountMail.MaxLength} caracteres.")
            .EmailAddress()
            .WithMessage("El correo de la cuenta no tiene un formato válido.");

        RuleFor(command => command.Status)
            .NotEmpty()
            .WithMessage("El estado de la cuenta es obligatorio.")
            .MaximumLength(40)
            .WithMessage("El estado de la cuenta no puede superar los 40 caracteres.")
            .Must(status => AccountStatus.AllowedValues.Contains(status, StringComparer.Ordinal))
            .WithMessage(
                $"El estado de la cuenta debe ser uno de: {string.Join(", ", AccountStatus.AllowedValues)}.");
    }
}
