using Domain.AccountStatements.ValueObjects;
using FluentValidation;

namespace Application.AccountStatements.UseCases;

public sealed class CreateAccountStatementCommandValidator
    : AbstractValidator<CreateAccountStatementCommand>
{
    public CreateAccountStatementCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty()
            .WithMessage("Debe asociar el estado de cuenta a una cuenta.");

        RuleFor(command => command.IssueDate)
            .NotEmpty()
            .WithMessage("La fecha de emisión es obligatoria.");

        RuleFor(command => command.Status)
            .NotEmpty()
            .WithMessage("El estado del estado de cuenta es obligatorio.")
            .MaximumLength(StatementStatus.MaxLength)
            .WithMessage(
                $"El estado no puede superar los {StatementStatus.MaxLength} caracteres.");
    }
}
