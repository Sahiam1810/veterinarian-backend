using Application.Common.Abstractions;
using FluentValidation;

namespace Application.Diagnostics.UseCases;

public sealed class UpdateDiagnosticCommandValidator : AbstractValidator<UpdateDiagnosticCommand>
{
    public UpdateDiagnosticCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id es requerido.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es requerido.")
            .MaximumLength(15).WithMessage("El código no puede exceder 15 caracteres.")
            .MustAsync(async (command, code, cancellationToken) =>
                !await unitOfWork.DiagnosticsRepository.ExistsCodeForDifferentIdAsync(
                    command.Id,
                    code.Trim().ToUpper(),
                    cancellationToken))
            .WithMessage("Ya existe otro diagnóstico con ese código.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");
    }
}
