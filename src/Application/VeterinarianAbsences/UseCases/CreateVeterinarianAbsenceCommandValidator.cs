using Domain.VeterinarianAbsences.Entities;
using FluentValidation;

namespace Application.VeterinarianAbsences.UseCases;

public sealed class CreateVeterinarianAbsenceCommandValidator
    : AbstractValidator<CreateVeterinarianAbsenceCommand>
{
    public CreateVeterinarianAbsenceCommandValidator()
    {
        RuleFor(x => x.VeterinarianId)
            .NotEmpty().WithMessage("El veterinario es requerido.");

        RuleFor(x => x.EndAtUtc)
            .GreaterThan(x => x.StartAtUtc)
            .WithMessage("La fecha de fin de la ausencia debe ser posterior al inicio.");

        RuleFor(x => x.Reason)
            .MaximumLength(VeterinarianAbsence.ReasonMaxLength)
            .WithMessage($"El motivo no puede superar los {VeterinarianAbsence.ReasonMaxLength} caracteres.");
    }
}
