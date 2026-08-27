using FluentValidation;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class UpdateAppointmentStatusHistoryCommandValidator : AbstractValidator<UpdateAppointmentStatusHistoryCommand>
{
    public UpdateAppointmentStatusHistoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id del historial es requerido.");

        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("La cita médica es requerida.");

        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("El estado es requerido.");

        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("La relación cliente-mascota es requerida.");

        RuleFor(x => x.Comment)
            .MaximumLength(100).WithMessage("El comentario no puede exceder 100 caracteres.");
    }
}
