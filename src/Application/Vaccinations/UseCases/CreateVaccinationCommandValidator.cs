using FluentValidation;

namespace Application.Vaccinations.UseCases;

public sealed class CreateVaccinationCommandValidator : AbstractValidator<CreateVaccinationCommand>
{
    public CreateVaccinationCommandValidator()
    {
        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("La relación cliente-mascota es requerida.");

        RuleFor(x => x.RecordId)
            .NotEmpty().WithMessage("La historia médica es requerida.");

        RuleFor(x => x.VaccineName)
            .NotEmpty().WithMessage("El nombre de la vacuna es requerido.")
            .MaximumLength(30).WithMessage("El nombre de la vacuna no puede exceder 30 caracteres.");

        RuleFor(x => x.DoseNumber)
            .GreaterThan(0).WithMessage("El número de dosis debe ser mayor a 0.");

        RuleFor(x => x.ApplicationDate)
            .NotEmpty().WithMessage("La fecha de aplicación es requerida.");

        RuleFor(x => x.NextDoseDate)
            .GreaterThan(x => x.ApplicationDate).When(x => x.NextDoseDate.HasValue)
            .WithMessage("La fecha de la próxima dosis debe ser posterior a la fecha de aplicación.");
    }
}
