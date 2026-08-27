using Domain.AccountStatements.ValueObjects;
using FluentValidation;

namespace Application.AccountStatements.UseCases;

public sealed class UpdateAccountStatementStatusCommandValidator
    : AbstractValidator<UpdateAccountStatementStatusCommand>
{
    public UpdateAccountStatementStatusCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Status)
            .NotEmpty()
            .WithMessage("El estado del estado de cuenta es obligatorio.")
            .MaximumLength(StatementStatus.MaxLength)
            .WithMessage(
                $"El estado no puede superar los {StatementStatus.MaxLength} caracteres.");
    }
}
