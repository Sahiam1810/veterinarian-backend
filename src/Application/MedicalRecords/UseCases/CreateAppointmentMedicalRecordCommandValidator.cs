using FluentValidation;

namespace Application.MedicalRecords.UseCases;

public sealed class CreateAppointmentMedicalRecordCommandValidator
    : AbstractValidator<CreateAppointmentMedicalRecordCommand>
{
    public CreateAppointmentMedicalRecordCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("La cita médica es requerida.");

        RuleFor(x => x.DiagnosticId)
            .NotEmpty().WithMessage("El diagnóstico es requerido.");

        RuleFor(x => x.ActorUserAccountId)
            .NotEmpty().WithMessage("La cuenta de usuario es requerida.");

        RuleFor(x => x.Symptoms)
            .MaximumLength(30).WithMessage("Los síntomas no pueden exceder 30 caracteres.");

        RuleFor(x => x.Treatment)
            .MaximumLength(30).WithMessage("El tratamiento no puede exceder 30 caracteres.");

        RuleFor(x => x.WeightAtVisit)
            .GreaterThan(0).When(x => x.WeightAtVisit.HasValue)
            .WithMessage("El peso debe ser mayor a 0.");

        RuleFor(x => x.Temperature)
            .GreaterThan(0).When(x => x.Temperature.HasValue)
            .WithMessage("La temperatura debe ser mayor a 0.");

        RuleForEach(x => x.Vaccinations)
            .ChildRules(vaccination =>
            {
                vaccination.RuleFor(x => x.VaccineName)
                    .NotEmpty().WithMessage("El nombre de la vacuna es requerido.")
                    .MaximumLength(30).WithMessage("El nombre de la vacuna no puede exceder 30 caracteres.");

                vaccination.RuleFor(x => x.DoseNumber)
                    .GreaterThan(0).WithMessage("El número de dosis debe ser mayor a 0.");

                vaccination.RuleFor(x => x.ApplicationDate)
                    .NotEmpty().WithMessage("La fecha de aplicación es requerida.");

                vaccination.RuleFor(x => x.NextDoseDate)
                    .GreaterThan(x => x.ApplicationDate).When(x => x.NextDoseDate.HasValue)
                    .WithMessage("La fecha de la próxima dosis debe ser posterior a la fecha de aplicación.");
            })
            .When(x => x.Vaccinations is not null);
    }
}
