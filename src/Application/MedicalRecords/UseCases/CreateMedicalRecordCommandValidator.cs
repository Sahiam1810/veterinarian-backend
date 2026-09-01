using FluentValidation;

namespace Application.MedicalRecords.UseCases;

public sealed class CreateMedicalRecordCommandValidator : AbstractValidator<CreateMedicalRecordCommand>
{
    public CreateMedicalRecordCommandValidator()
    {
        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("La relación cliente-mascota es requerida.");

        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("La cita médica es requerida.");

        RuleFor(x => x.DiagnosticId)
            .NotEmpty().WithMessage("El diagnóstico es requerido.");

        RuleFor(x => x.UserAccountId)
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
    }
}
