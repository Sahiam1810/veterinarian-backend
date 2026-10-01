using FluentValidation;

namespace Application.Appointments.UseCases;

public sealed class RecordAppointmentVitalsCommandValidator : AbstractValidator<RecordAppointmentVitalsCommand>
{
    public RecordAppointmentVitalsCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("La cita médica es requerida.");

        RuleFor(x => x.Weight)
            .GreaterThan(0).When(x => x.Weight.HasValue)
            .WithMessage("El peso debe ser mayor a 0.");

        RuleFor(x => x.Temperature)
            .GreaterThan(0).When(x => x.Temperature.HasValue)
            .WithMessage("La temperatura debe ser mayor a 0.");

        RuleFor(x => x.HeartRate)
            .GreaterThan(0).When(x => x.HeartRate.HasValue)
            .WithMessage("La frecuencia cardíaca debe ser mayor a 0.");

        RuleFor(x => x.RespiratoryRate)
            .GreaterThan(0).When(x => x.RespiratoryRate.HasValue)
            .WithMessage("La frecuencia respiratoria debe ser mayor a 0.");
    }
}
