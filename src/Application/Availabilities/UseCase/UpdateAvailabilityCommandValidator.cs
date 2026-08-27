using Application.Common.Abstractions;
using FluentValidation;

namespace Application.Availabilities.UseCase;

public sealed class UpdateAvailabilityCommandValidator : AbstractValidator<UpdateAvailabilityCommand>
{
    public UpdateAvailabilityCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id de la disponibilidad es requerido.");

        RuleFor(x => x.VeterinarianId)
            .NotEmpty().WithMessage("El veterinario es requerido.");

        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("El día de la semana no es válido.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("La hora de fin debe ser posterior a la hora de inicio.");

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
                !await unitOfWork.AvailabilitiesRepository.ExistsOverlapAsync(
                    command.VeterinarianId,
                    command.DayOfWeek,
                    command.StartTime,
                    command.EndTime,
                    cancellationToken,
                    command.Id))
            .WithMessage("Ya existe otra disponibilidad que se cruza con ese horario para este veterinario.");
    }
}
