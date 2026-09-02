using FluentValidation;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentStatusCommandValidator
    : AbstractValidator<UpdateAppointmentStatusCommand>
{
    public UpdateAppointmentStatusCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("La cita médica es requerida.");

        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("El estado es requerido.");

        RuleFor(x => x.Comment)
            .MaximumLength(100).WithMessage("El comentario no puede exceder 100 caracteres.");
    }
}
