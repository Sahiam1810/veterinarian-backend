using Application.Common.Abstractions;
using Domain.Availabilities.ValueObjects;
using FluentValidation;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id de la cita es requerido.");

        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("La relacion cliente-mascota es requerida.");

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

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden exceder 500 caracteres.");

        RuleFor(x => x.ConsultingRoom)
            .MaximumLength(ConsultingRoom.MaxLength)
            .WithMessage($"El consultorio no puede superar los {ConsultingRoom.MaxLength} caracteres.");

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
            {
                // Sin servicio o sin duración válida no hay fin calculable; el handler lo rechaza.
                var service = await unitOfWork.ServicesRepository.GetByIdAsync(
                    command.ServiceId,
                    cancellationToken);
                if (service is null || service.DurationMinutes <= 0)
                {
                    return true;
                }

                return !await unitOfWork.AppointmentsRepository.HasOverlappingAppointmentAsync(
                    command.ClientPetId,
                    command.VeterinarianId,
                    command.ScheduledStart,
                    command.ScheduledStart.AddMinutes(service.DurationMinutes),
                    excludeAppointmentId: command.Id,
                    cancellationToken: cancellationToken);
            })
            .WithMessage("Ya existe otra cita agendada para la mascota o el veterinario en el horario seleccionado.");
    }
}
