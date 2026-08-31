using Application.Common.Abstractions;
using FluentValidation;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id de la cita es requerido.");

        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("La relación cliente-mascota es requerida.");

        RuleFor(x => x.VeterinarianId)
            .NotEmpty().WithMessage("El veterinario es requerido.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("El servicio es requerido.");

        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("El estado de la cita es requerido.");

        RuleFor(x => x.AvailabilityId)
            .NotEmpty().WithMessage("La disponibilidad es requerida.");

        RuleFor(x => x.ScheduledStart)
            .NotEmpty().WithMessage("La fecha de inicio es requerida.");

        RuleFor(x => x.ScheduledEnd)
            .NotEmpty().WithMessage("La fecha de fin es requerida.")
            .GreaterThan(x => x.ScheduledStart)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.Notes)
            .MaximumLength(100).WithMessage("Las notas no pueden exceder 100 caracteres.");

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
                !await unitOfWork.AppointmentsRepository.HasOverlappingAppointmentAsync(
                    command.ClientPetId,
                    command.VeterinarianId,
                    command.ScheduledStart,
                    command.ScheduledEnd,
                    excludeAppointmentId: command.Id,
                    cancellationToken: cancellationToken))
            .WithMessage("Ya existe otra cita agendada para la mascota o el veterinario en el horario seleccionado.");
    }
}
