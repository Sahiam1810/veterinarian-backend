using Application.Common.Abstractions;
using Domain.Availabilities.ValueObjects;
using FluentValidation;

namespace Application.Appointments.UseCases;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator(IUnitOfWork unitOfWork)
    {
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
            .NotEmpty().WithMessage("La fecha y hora de inicio son requeridas.");

        RuleFor(x => x.ScheduledEnd)
            .NotEmpty().WithMessage("La fecha y hora de fin son requeridas.")
            .GreaterThan(x => x.ScheduledStart)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

        RuleFor(x => x.Notes)
            .MaximumLength(100).WithMessage("Las notas no pueden exceder 100 caracteres.");

        RuleFor(x => x.ConsultingRoom)
            .MaximumLength(ConsultingRoom.MaxLength)
            .WithMessage($"El consultorio no puede superar los {ConsultingRoom.MaxLength} caracteres.");

        // Vacio permitido: el handler toma el telefono del perfil del dueno.
        RuleFor(x => x.RequesterPhoneNumber)
            .Must(phone =>
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return true;
                }

                try
                {
                    _ = Domain.Appointments.ValueObjects.RequesterPhoneNumber.Create(phone);
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            })
            .WithMessage("El telefono del solicitante no es valido.");

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
                !await unitOfWork.AppointmentsRepository.HasOverlappingAppointmentAsync(
                    command.ClientPetId,
                    command.VeterinarianId,
                    command.ScheduledStart,
                    command.ScheduledEnd,
                    cancellationToken: cancellationToken))
            .WithMessage("Ya existe una cita agendada para la mascota o el veterinario en el horario seleccionado.");
    }
}
